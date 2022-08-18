using System.Configuration;

namespace NET6_Template
{
    public static class ConfigBase
    {
        private static AppSettingsReader settingsReader = new AppSettingsReader();

        public static string ReadSetting(string settingName)
        {
            return ReadSettingInternal(settingName, string.Empty);
        }

        static string ReadSettingInternal(string settingName, string defaultValue)
        {
            string retValue = string.Empty;

            try
            {
                settingsReader = settingsReader ?? new AppSettingsReader();
                retValue = System.Environment.GetEnvironmentVariable(settingName) ??
                    (string)settingsReader.GetValue(settingName, retValue.GetType());
            }
            catch
            {
                retValue = defaultValue;
            }

            return retValue;
        }
    }
}
