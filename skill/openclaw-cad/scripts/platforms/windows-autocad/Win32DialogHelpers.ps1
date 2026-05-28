# Windows 原生对话框辅助（user32.dll）。仅 Windows + 本机 AutoCAD 可用。

if ($PSVersionTable.PSPlatform -eq 'Unix') {
    throw 'Win32DialogHelpers.ps1 requires Windows (user32.dll).'
}

if (-not ('Win32Dialog' -as [type])) {
    Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public static class Win32Dialog
{
    public const uint WM_SETTEXT = 0x000C;
    public const uint WM_KEYDOWN = 0x0100;
    public const uint WM_KEYUP = 0x0101;
    public const uint BM_CLICK = 0x00F5;
    public const int VK_RETURN = 0x0D;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string lpszClass, string lpszWindow);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);

    public delegate bool EnumChildProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);
}
"@
}

function Get-WindowTextSafe {
    param([IntPtr]$Handle)
    if ($Handle -eq [IntPtr]::Zero) { return $null }
    $sb = New-Object System.Text.StringBuilder 512
    [void][Win32Dialog]::GetWindowText($Handle, $sb, $sb.Capacity)
    return $sb.ToString()
}

function Wait-DialogWindow {
    param(
        [string[]]$Titles,
        [int]$TimeoutMs = 5000,
        [int]$PollMs = 100
    )
    $deadline = [Environment]::TickCount64 + $TimeoutMs
    while ([Environment]::TickCount64 -lt $deadline) {
        foreach ($title in $Titles) {
            $h = [Win32Dialog]::FindWindow('#32770', $title)
            if ($h -ne [IntPtr]::Zero -and [Win32Dialog]::IsWindowVisible($h)) {
                return $h
            }
        }
        Start-Sleep -Milliseconds $PollMs
    }
    return [IntPtr]::Zero
}

function Get-FileDialogEditControl {
    param([IntPtr]$DialogHandle)
    if ($DialogHandle -eq [IntPtr]::Zero) { return [IntPtr]::Zero }

    $combo = [Win32Dialog]::FindWindowEx($DialogHandle, [IntPtr]::Zero, 'ComboBoxEx32', $null)
    if ($combo -ne [IntPtr]::Zero) {
        $edit = [Win32Dialog]::FindWindowEx($combo, [IntPtr]::Zero, 'Edit', $null)
        if ($edit -ne [IntPtr]::Zero) { return $edit }
    }

    return [Win32Dialog]::FindWindowEx($DialogHandle, [IntPtr]::Zero, 'Edit', $null)
}

function Set-DialogEditText {
    param(
        [IntPtr]$EditHandle,
        [string]$Text
    )
    if ($EditHandle -eq [IntPtr]::Zero) {
        throw 'File dialog Edit control not found.'
    }
    [void][Win32Dialog]::SendMessage($EditHandle, [Win32Dialog]::WM_SETTEXT, [IntPtr]::Zero, $Text)
}

function Submit-DialogWithEnter {
    param([IntPtr]$EditHandle)
    if ($EditHandle -eq [IntPtr]::Zero) {
        throw 'Edit control not found for Enter submit.'
    }
    $vk = [IntPtr]::new([Win32Dialog]::VK_RETURN)
    [void][Win32Dialog]::PostMessage($EditHandle, [Win32Dialog]::WM_KEYDOWN, $vk, [IntPtr]::Zero)
    [void][Win32Dialog]::PostMessage($EditHandle, [Win32Dialog]::WM_KEYUP, $vk, [IntPtr]::Zero)
}

function Find-DialogButton {
    param(
        [IntPtr]$DialogHandle,
        [string[]]$ButtonTexts
    )
    if ($DialogHandle -eq [IntPtr]::Zero) { return [IntPtr]::Zero }

    foreach ($text in $ButtonTexts) {
        $h = [Win32Dialog]::FindWindowEx($DialogHandle, [IntPtr]::Zero, 'Button', $text)
        if ($h -ne [IntPtr]::Zero) { return $h }
    }

    return [IntPtr]::Zero
}

function Click-DialogButton {
    param([IntPtr]$ButtonHandle)
    if ($ButtonHandle -eq [IntPtr]::Zero) {
        throw 'Dialog button not found.'
    }
    [void][Win32Dialog]::PostMessage($ButtonHandle, [Win32Dialog]::BM_CLICK, [IntPtr]::Zero, [IntPtr]::Zero)
}

function Get-NetloadDialogTitles {
    # 选择 .NET 程序集 / Select .NET Assembly（避免 .ps1 内中文引号编码问题）
    $zh = -join @(
        [char]0x9009, [char]0x62E9, ' ', '.NET', ' ',
        [char]0x7A0B, [char]0x5E8F, [char]0x96C6
    )
    return @($zh, 'Select .NET Assembly')
}

function Get-SecurityDialogTitles {
    $zh = -join @(
        [char]0x5B89, [char]0x5168, [char]0x6027, ' - ',
        [char]0x672A, [char]0x7B7E, [char]0x540D, [char]0x7684, [char]0x53EF, [char]0x6267, [char]0x884C, [char]0x6587, [char]0x4EF6
    )
    return @($zh, 'Security - Unsigned Executable File')
}

function Get-AlwaysLoadButtonTexts {
    $zh = -join @([char]0x59CB, [char]0x7EC8, [char]0x52A0, [char]0x8F7D)
    return @($zh, 'Always Load')
}

function Invoke-NetloadFileDialogAutomation {
    param(
        [string]$DllPath,
        [int]$DialogWaitMs = 5000,
        [int]$SecurityWaitMs = 3000
    )

    $fullPath = [System.IO.Path]::GetFullPath($DllPath)
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw "DLL not found: $fullPath"
    }

    $netloadTitles = Get-NetloadDialogTitles
    $securityTitles = Get-SecurityDialogTitles
    $alwaysLoadTexts = Get-AlwaysLoadButtonTexts

    $dlg = Wait-DialogWindow -Titles $netloadTitles -TimeoutMs $DialogWaitMs
    if ($dlg -eq [IntPtr]::Zero) {
        throw 'NETLOAD file dialog did not appear in time.'
    }

    $edit = Get-FileDialogEditControl -DialogHandle $dlg
    Set-DialogEditText -EditHandle $edit -Text $fullPath
    Submit-DialogWithEnter -EditHandle $edit

    Start-Sleep -Milliseconds 300

    $secDlg = Wait-DialogWindow -Titles $securityTitles -TimeoutMs $SecurityWaitMs
    if ($secDlg -ne [IntPtr]::Zero) {
        $btn = Find-DialogButton -DialogHandle $secDlg -ButtonTexts $alwaysLoadTexts
        if ($btn -ne [IntPtr]::Zero) {
            Click-DialogButton -ButtonHandle $btn
        }
    }

    return @{
        ok             = $true
        dllPath        = $fullPath
        netloadDialog  = $true
        securityDialog = ($secDlg -ne [IntPtr]::Zero)
    }
}
