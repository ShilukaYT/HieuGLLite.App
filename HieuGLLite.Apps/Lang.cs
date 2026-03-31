using System;
using System.Resources;

namespace HieuGLLite.Apps
{
    // Simple strongly-typed wrapper for resource strings used across the app.
    internal static class Lang
    {
        private static readonly ResourceManager rm = new ResourceManager("HieuGLLite.Apps.Lang", typeof(Lang).Assembly);

        private static string Get(string name, string fallback = null)
        {
            try
            {
                var v = rm.GetString(name);
                if (!string.IsNullOrEmpty(v)) return v;
            }
            catch { }
            return fallback ?? name;
        }

        public static string AppName => Get("AppName", "Hieu GL Lite");
        public static string TrayOpen => Get("TrayOpen", "Open");
        public static string TrayExit => Get("TrayExit", "Exit completely");
        public static string MsgErrInitWebview => Get("MsgErrInitWebview", "WebView2 initialization failed: ");
        public static string MsgErrTitle => Get("MsgErrTitle", "Error");
        public static string MsgErrOpenLink => Get("MsgErrOpenLink", "Unable to open the link: ");
        public static string MsgErrOpenBrowser => Get("MsgErrOpenBrowser", "Unable to open the browser: ");
        public static string MsgErrTokenExchange => Get("MsgErrTokenExchange", "Token exchange failed: ");
        public static string MsgApiErrorTitle => Get("MsgApiErrorTitle", "API Error");
        public static string MsgNotJoinedServer => Get("MsgNotJoinedServer", "You have not joined the Hieu GL Lite server\nApplication features may be limited!");
        public static string MsgNoticeTitle => Get("MsgNoticeTitle", "Notice");
        public static string MsgWarnExitInstalling => Get("MsgWarnExitInstalling", "The application is currently installing, you cannot exit at this time.");
        public static string MsgWarnTitle => Get("MsgWarnTitle", "Warning");
        public static string MsgWarnExitRunning => Get("MsgWarnExitRunning", "An application is running, you cannot exit completely at this time.");
        public static string MsgConfirmCancelInstall => Get("MsgConfirmCancelInstall", "Are you sure you want to cancel downloading this application?\nAll progress and temporary data will be cleared!");
        public static string MsgConfirmTitle => Get("MsgConfirmTitle", "Confirm");
        public static string MsgConfirmCancelTitle => Get("MsgConfirmCancelTitle", "Confirm cancellation");
        public static string MsgConfirmUninstall => Get("MsgConfirmUninstall", "Are you sure you want to uninstall this application?\nAll apps and games installed on this application will be uninstalled!");
        public static string MsgConfirmCleanup => Get("MsgConfirmCleanup", "Do you want to clean up?\nDownloaded and temporary data will be deleted!");
        public static string MsgWarnCleanupDownloading => Get("MsgWarnCleanupDownloading", "There is a file downloading, you cannot clean up right now!");
        public static string MsgWarnCannotCleanupTitle => Get("MsgWarnCannotCleanupTitle", "Cannot clean up");
        public static string MsgConfirmUninstallBase => Get("MsgConfirmUninstallBase", "Do you want to uninstall this application?\nSome emulators will not be able to start without this application?");
        public static string MsgConfirmUninstallBaseTitle => Get("MsgConfirmUninstallBaseTitle", "Confirm uninstallation");
        public static string MsgErrRequireAdmin => Get("MsgErrRequireAdmin", "Administrator privileges (Run as Administrator) are required to completely remove the system protocol.");
        public static string MsgErrMissingPrivilegeTitle => Get("MsgErrMissingPrivilegeTitle", "Missing privilege");
        public static string MsgErrRegistry => Get("MsgErrRegistry", "Registry removal error: ");
        public static string MsgErrUpdateNotFound => Get("MsgErrUpdateNotFound", "Installation file not found, please download again!");
        public static string MsgErrSaveToken => Get("MsgErrSaveToken", "Error saving token: ");
        public static string MsgErrExecuteFile => Get("MsgErrExecuteFile", "Execution error: ");
        public static string MsgErrToolNotFound => Get("MsgErrToolNotFound", "Tool not found: ");
        public static string MsgErrMissingInstallFiles => Get("MsgErrMissingInstallFiles", "Installation failed, installation files not found!");
        public static string MsgErrInstallFailedTitle => Get("MsgErrInstallFailedTitle", "Installation failed");
        public static string MsgErrHashMismatch => Get("MsgErrHashMismatch", "Integrity check failed, please try again later!\n\nFile status details:\n- Setup file: {0}\n- OS file: {1}");
        public static string MsgHashMismatchTitle => Get("MsgHashMismatchTitle", "SHA256 validation error");
        public static string FileValid => Get("FileValid", "Valid");
        public static string FileCorrupted => Get("FileCorrupted", "Corrupted/Broken");
        public static string MsgErrInstallRolledBack => Get("MsgErrInstallRolledBack", "Installation failed, the installer has undone changes");
        public static string MsgWarnAnotherInstalling => Get("MsgWarnAnotherInstalling", "There is another application currently being processed!");
        public static string MsgErrUnauthorizedAcess => Get("MsgErrUnauthorizedAcess", "Unauthorized access detected!\nYou have not completed OTP verification.");
        public static string MsgErrSecurityTitle => Get("MsgErrSecurityTitle", "System security");
        public static string MsgWarnInsufficientSpace => Get("MsgWarnInsufficientSpace", "Drive {0} does not have enough space!\n- Required: {1:F2} GB\n- Available: {2:F2} GB");
        public static string MsgWarnChangeVersion => Get("MsgWarnChangeVersion", "You are changing the version for {0}.\n\nThis action will WIPE OUT all data, applications, and settings of the current version before installing the new one.\n\nAre you sure you want to continue?");
        public static string MsgWarnChangeVersionTitle => Get("MsgWarnChangeVersionTitle", "Version change warning");
        public static string MsgWarnConflictFound => Get("MsgWarnConflictFound", "Your PC currently has {0} installed which conflicts with this version.\n\nDo you want to uninstall it to continue installing {1}?");
        public static string MsgWarnConflictTitle => Get("MsgWarnConflictTitle", "Conflict detected");
        public static string MsgWarnKillApp => Get("MsgWarnKillApp", "Are you sure you want to close this application?\nYour game progress might not be saved!");

    public static string btnYes => Get("btnYes", "Yes");
    public static string btnNo => Get("btnNo", "No");
    public static string btnGotit => Get("btnGotit", "Got it");
    public static string btnOK=> Get("btnOK", "OK");

    public static string MsgRestartAfterChangeLanguage => Get("MsgRestartAfterChangeLanguage", "For the new language to take full effect, you should restart the application!");
    public static string MsgRestartAfterChangeLanguageTitle => Get("MsgRestartAfterChangeLanguageTitle", "Need restart");

    //toast
    public static string ToastInstallMessage => Get("ToastInstallMessage", "{0} has been installed");
    public static string ToastInstallTitle => Get("ToastInstallTitle", "Installation complete");

    public static string ToastUninstallMessage => Get("ToastUninstallMessage", "{0} has been uninstalled");
    public static string ToastUninstallTitle => Get("ToastUninstallTitle", "Uninstallation complete");




		// Fallback accessor
		public static string GetString(string key) => Get(key);
    }
}
 