using System.Xml;

namespace CrmSvcUtil.Client
{
    /// <summary>
    ///     Configuration Helper.
    /// </summary>
    /// <remarks>
    ///     Johan Küstner, 07/03/2022.
    /// </remarks>
    public class ConfigHelper
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConfigHelper"/> class.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <param name="inputPath">
        ///     The inputPath.
        /// </param>
        public ConfigHelper(string inputPath)
        {
            InputPath = inputPath;
            ConfigFile = new XmlDocument();
            ConfigFile.Load(InputPath);
        }

        /// <summary>
        ///     Adds the key value pair to configuration.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 07/03/2022.
        /// </remarks>
        /// <param name="configKey">
        ///     The configuration key.
        /// </param>
        /// <param name="configValue">
        ///     The configuration value.
        /// </param>
        public void AddKeyValuePairToConfig(string configKey, string configValue)
        {
            if (ConfigFile.DocumentElement == null)
            {
                return;
            }
            if (ConfigFile.DocumentElement.Name != "configuration")
            {
                return;
            }
            bool hasAppSettings = false;
            foreach (XmlElement xmlElement in ConfigFile.DocumentElement.ChildNodes)
            {
                if (xmlElement.Name == "appSettings")
                {
                    hasAppSettings = true;
                }
            }
            if (!hasAppSettings)
            {
                XmlElement xmlElement = ConfigFile.CreateElement("appSettings");
                ConfigFile.DocumentElement?.AppendChild(xmlElement);
            }
            if (ConfigFile.DocumentElement != null)
            {
                foreach (XmlElement xmlElement in ConfigFile.DocumentElement.ChildNodes)
                {
                    if (xmlElement.Name != "appSettings")
                    {
                        continue;
                    }
                    XmlElement childXmlElement = ConfigFile.CreateElement("add");
                    XmlAttribute keyXmlAttribute = ConfigFile.CreateAttribute("key");
                    keyXmlAttribute.Value = configKey;
                    childXmlElement.Attributes.Append(keyXmlAttribute);
                    XmlAttribute valueXmlAttribute = ConfigFile.CreateAttribute("value");
                    valueXmlAttribute.Value = configValue;
                    childXmlElement.Attributes.Append(valueXmlAttribute);
                    xmlElement.AppendChild(childXmlElement);
                }
            }
            ConfigFile.Save(InputPath);
        }

        /// <summary>
        ///     Gets the configuration file.
        /// </summary>
        /// <value>
        ///     The configuration file.
        /// </value>
        private XmlDocument ConfigFile { get; }

        /// <summary>
        ///     Gets the inputPath.
        /// </summary>
        /// <value>
        ///     The inputPath.
        /// </value>
        private string InputPath { get; }
    }
}
