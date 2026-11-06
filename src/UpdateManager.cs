using System;
using System.Globalization;
using Microsoft.Win32;

namespace WindowsUpdateController
{
    /// <summary>一次读取出来的全部状态</summary>
    public sealed class UpdateStatus
    {
        public string WindowsVersion;
        public string BuildNumber;
        public string Edition;

        public DateTime? PauseStart;
        public DateTime? PauseEnd;
        public int? MaxPauseDays;

        public bool PauseActive;
        public bool PauseExpired;

        public bool VersionLocked;
        public string LockedVersion;
    }

    /// <summary>
    /// 只负责读写注册表,不做任何界面相关的事情。
    /// 三个功能和之前 PowerShell 脚本完全一致:
    ///   暂停 = PauseQualityUpdatesStartTime/EndTime 等 + FlightSettingsMaxPauseDays
    ///   锁版本 = TargetReleaseVersion / TargetReleaseVersionInfo
    ///   恢复 = 删除上述全部值
    /// </summary>
    public static class UpdateManager
    {
        private const string UxKey     = @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings";
        private const string PolicyKey = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";
        private const string CvKey     = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

        private const string IsoFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

        private static readonly string[] PauseValues = new string[]
        {
            "PauseFeatureUpdatesStartTime",
            "PauseFeatureUpdatesEndTime",
            "PauseQualityUpdatesStartTime",
            "PauseQualityUpdatesEndTime",
            "PauseUpdatesExpiryTime",
            "FlightSettingsMaxPauseDays"
        };

        private static readonly string[] LockValues = new string[]
        {
            "TargetReleaseVersion",
            "TargetReleaseVersionInfo"
        };

        public static UpdateStatus GetStatus()
        {
            UpdateStatus s = new UpdateStatus();

            using (RegistryKey cv = Registry.LocalMachine.OpenSubKey(CvKey))
            {
                if (cv != null)
                {
                    s.WindowsVersion = cv.GetValue("DisplayVersion") as string;
                    object build = cv.GetValue("CurrentBuild");
                    object ubr = cv.GetValue("UBR");
                    s.BuildNumber = (build == null ? "?" : build.ToString()) + "." + (ubr == null ? "?" : ubr.ToString());
                    s.Edition = MapEdition(cv.GetValue("EditionID") as string);
                }
            }

            using (RegistryKey ux = Registry.LocalMachine.OpenSubKey(UxKey))
            {
                if (ux != null)
                {
                    s.PauseStart = ParseUtc(ux.GetValue("PauseQualityUpdatesStartTime") as string);
                    s.PauseEnd = ParseUtc(ux.GetValue("PauseQualityUpdatesEndTime") as string);
                    object max = ux.GetValue("FlightSettingsMaxPauseDays");
                    if (max is int)
                    {
                        s.MaxPauseDays = (int)max;
                    }
                }
            }

            if (s.PauseEnd.HasValue)
            {
                if (s.PauseEnd.Value > DateTime.Now)
                {
                    s.PauseActive = true;
                }
                else
                {
                    s.PauseExpired = true;
                }
            }

            using (RegistryKey pol = Registry.LocalMachine.OpenSubKey(PolicyKey))
            {
                if (pol != null)
                {
                    object flag = pol.GetValue("TargetReleaseVersion");
                    s.LockedVersion = pol.GetValue("TargetReleaseVersionInfo") as string;
                    s.VersionLocked = (flag is int) && ((int)flag == 1) && !string.IsNullOrEmpty(s.LockedVersion);
                }
            }

            return s;
        }

        /// <summary>暂停更新。lockVersion 为 true 时同时锁定当前功能更新版本。</summary>
        public static void Pause(int days, bool lockVersion)
        {
            if (days < 1)
            {
                days = 1;
            }
            if (days > 3650)
            {
                days = 3650;
            }

            DateTime now = DateTime.UtcNow;
            DateTime end = now.AddDays(days);
            string startStr = now.ToString(IsoFormat, CultureInfo.InvariantCulture);
            string endStr = end.ToString(IsoFormat, CultureInfo.InvariantCulture);

            using (RegistryKey ux = Registry.LocalMachine.CreateSubKey(UxKey))
            {
                if (ux == null)
                {
                    throw new InvalidOperationException("无法打开注册表项:" + UxKey);
                }

                // 默认上限 35 天,调大后截止时间才不会被系统截断
                ux.SetValue("FlightSettingsMaxPauseDays", days + 30, RegistryValueKind.DWord);

                ux.SetValue("PauseFeatureUpdatesStartTime", startStr, RegistryValueKind.String);
                ux.SetValue("PauseQualityUpdatesStartTime", startStr, RegistryValueKind.String);
                ux.SetValue("PauseFeatureUpdatesEndTime", endStr, RegistryValueKind.String);
                ux.SetValue("PauseQualityUpdatesEndTime", endStr, RegistryValueKind.String);
                ux.SetValue("PauseUpdatesExpiryTime", endStr, RegistryValueKind.String);
            }

            string lockedVersion = null;
            if (lockVersion)
            {
                lockedVersion = GetStatus().WindowsVersion;
                if (!string.IsNullOrEmpty(lockedVersion))
                {
                    using (RegistryKey pol = Registry.LocalMachine.CreateSubKey(PolicyKey))
                    {
                        if (pol != null)
                        {
                            pol.SetValue("TargetReleaseVersion", 1, RegistryValueKind.DWord);
                            pol.SetValue("TargetReleaseVersionInfo", lockedVersion, RegistryValueKind.String);
                        }
                    }
                }
            }

            // 回读校验,确保真的写进去了
            UpdateStatus check = GetStatus();
            if (!check.PauseActive)
            {
                throw new InvalidOperationException("写入注册表后暂停状态没有生效,请确认程序以管理员身份运行。");
            }
            if (lockVersion && !check.VersionLocked)
            {
                throw new InvalidOperationException("版本锁定没有生效,可能读不到当前功能更新版本号。");
            }
        }

        /// <summary>恢复:删除暂停与版本锁定的全部值。</summary>
        public static void Resume()
        {
            using (RegistryKey ux = Registry.LocalMachine.OpenSubKey(UxKey, true))
            {
                if (ux != null)
                {
                    foreach (string name in PauseValues)
                    {
                        ux.DeleteValue(name, false);
                    }
                }
            }

            using (RegistryKey pol = Registry.LocalMachine.OpenSubKey(PolicyKey, true))
            {
                if (pol != null)
                {
                    foreach (string name in LockValues)
                    {
                        pol.DeleteValue(name, false);
                    }
                }
            }
        }

        private static DateTime? ParseUtc(string iso)
        {
            if (string.IsNullOrEmpty(iso))
            {
                return null;
            }

            DateTime utc;
            if (DateTime.TryParse(iso, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out utc))
            {
                return utc.ToLocalTime();
            }

            return null;
        }

        private static string MapEdition(string id)
        {
            if (id == null)
            {
                return string.Empty;
            }

            switch (id)
            {
                case "Core": return "家庭版";
                case "CoreSingleLanguage": return "家庭单语言版";
                case "Professional": return "专业版";
                case "ProfessionalWorkstation": return "专业工作站版";
                case "ProfessionalEducation": return "专业教育版";
                case "Enterprise": return "企业版";
                case "EnterpriseS": return "企业版 LTSC";
                case "Education": return "教育版";
                default: return id;
            }
        }
    }
}
