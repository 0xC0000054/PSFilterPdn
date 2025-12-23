/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2025 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet;
using PaintDotNet.AppModel;
using PSFilterPdn.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace PSFilterPdn
{
    internal static class PSFilterPdnSettingsFile
    {
        private const string SettingsFileName = "PSFilterPdn.xml";

        public static PSFilterPdnSettings Load(IServiceProvider serviceProvider)
        {
            PSFilterPdnSettings settings;

            try
            {
                string userDataPath = serviceProvider.GetService<IUserFilesService>()?.UserFilesPath ?? throw new IOException(Resources.UnknownUserFilePath);

                string path = Path.Combine(userDataPath, SettingsFileName);

                using (FileStream stream = new(path, FileMode.Open, FileAccess.Read))
                {
                    XmlReaderSettings readerSettings = new()
                    {
                        CloseInput = false,
                        IgnoreComments = true,
                        XmlResolver = null
                    };

                    using (XmlReader xmlReader = XmlReader.Create(stream, readerSettings))
                    {
                        // Detect the old settings format and convert it to the new format.
                        if (xmlReader.MoveToContent() == XmlNodeType.Element &&
                            xmlReader.Name == OldXmlSettings.RootNodeName)
                        {
                            (List<string> directories, bool searchSubdirectories) = ParseOldSettingsFormat(xmlReader);

                            settings = new PSFilterPdnSettings
                            {
                                SearchDirectories = new HashSet<string>(directories, StringComparer.OrdinalIgnoreCase),
                                SearchSubdirectories = searchSubdirectories,
                                Dirty = true
                            };
                        }
                        else
                        {
                            DataContractSerializer serializer = new(typeof(PSFilterPdnSettings));
                            settings = (PSFilterPdnSettings)serializer.ReadObject(xmlReader)!;
                            settings.Dirty = false;
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is DirectoryNotFoundException || ex is FileNotFoundException)
            {
                // Use the default settings if the file is not present.
                settings = new PSFilterPdnSettings();
            }

            return settings;
        }

        public static void Save(IServiceProvider serviceProvider, PSFilterPdnSettings settings)
        {
            string userDataPath = serviceProvider.GetService<IUserFilesService>()?.UserFilesPath ?? throw new IOException(Resources.UnknownUserFilePath);

            DirectoryInfo info = new(userDataPath);

            if (!info.Exists)
            {
                info.Create();
            }

            string path = Path.Combine(userDataPath, SettingsFileName);

            XmlWriterSettings writerSettings = new()
            {
                Indent = true
            };

            using (XmlWriter writer = XmlWriter.Create(path, writerSettings))
            {
                DataContractSerializer serializer = new(typeof(PSFilterPdnSettings));
                serializer.WriteObject(writer, settings);
            }
        }

        private static (List<string>, bool) ParseOldSettingsFormat(XmlReader xmlReader)
        {
            List<string> directories = [];
            bool searchSubdirectories = true;

            XmlDocument xmlDocument = new();
            xmlDocument.Load(xmlReader);

            XmlNode? searchDirsNode = xmlDocument.SelectSingleNode(OldXmlSettings.SearchDirectoriesPath);
            if (searchDirsNode != null)
            {
                string dirs = searchDirsNode.InnerText.Trim();

                if (!string.IsNullOrEmpty(dirs))
                {
                    string[] splitDirs = dirs.Split([','], StringSplitOptions.None);

                    for (int i = 0; i < splitDirs.Length; i++)
                    {
                        string dir = splitDirs[i];

                        try
                        {
                            if (Path.IsPathRooted(dir))
                            {
                                directories.Add(dir);
                            }
                            else
                            {
                                // If the path contains a comma it will not be rooted
                                // append it to the previous path with a comma added.

                                int index = directories.Count - 1;
                                string lastPath = directories[index];

                                directories[index] = lastPath + "," + dir;
                            }
                        }
                        catch (ArgumentException)
                        {
                        }
                    }
                }
            }

            XmlNode? searchSubDirNode = xmlDocument.SelectSingleNode(OldXmlSettings.SearchSubdirectoriesPath);
            if (searchSubDirNode != null)
            {
                if (bool.TryParse(searchSubDirNode.InnerText.Trim(), out bool result))
                {
                    searchSubdirectories = result;
                }
            }

            return (directories, searchSubdirectories);
        }

        private static class OldXmlSettings
        {
            internal const string RootNodeName = "settings";
            internal const string SearchSubdirectoriesPath = "settings/searchSubDirs";
            internal const string SearchDirectoriesPath = "settings/searchDirs";
        }
    }
}
