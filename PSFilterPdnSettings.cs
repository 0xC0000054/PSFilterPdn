/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace PSFilterPdn
{
    [DataContract(Name = "PSFilterPdnSettings", Namespace = "")]
    internal sealed class PSFilterPdnSettings
    {
        private readonly string path;
        private bool changed;
        private bool createUserFilesDir;
        [DataMember(Name = "SearchDirectories")]
        private HashSet<string> searchDirectories;
        [DataMember(Name = "SearchSubdirectories")]
        private bool searchSubdirectories;

        /// <summary>
        /// Initializes a new instance of the <see cref="PSFilterPdnSettings"/> class.
        /// </summary>
        /// <param name="path">The path of the settings file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
        public PSFilterPdnSettings(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            this.path = path;
            changed = false;
            createUserFilesDir = false;
            searchSubdirectories = true;
            searchDirectories = null;
        }

        /// <summary>
        /// Gets or sets the search directories.
        /// </summary>
        /// <value>
        /// The search directories.
        /// </value>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null</exception>
        public HashSet<string> SearchDirectories
        {
            get => searchDirectories;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                searchDirectories = value;
                changed = true;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether subdirectories are searched.
        /// </summary>
        /// <value>
        ///   <c>true</c> if subdirectories are searched; otherwise, <c>false</c>.
        /// </value>
        public bool SearchSubdirectories
        {
            get => searchSubdirectories;
            set
            {
                if (searchSubdirectories != value)
                {
                    searchSubdirectories = value;
                    changed = true;
                }
            }
        }

        /// <summary>
        /// Saves any changes to this instance.
        /// </summary>
        public void Flush()
        {
            if (changed)
            {
                Save();
                changed = false;
            }
        }

        /// <summary>
        /// Loads the saved settings for this instance.
        /// </summary>
        public void LoadSavedSettings()
        {
            try
            {
                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    XmlReaderSettings readerSettings = new XmlReaderSettings
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
                            XmlDocument xmlDocument = new XmlDocument();
                            xmlDocument.Load(xmlReader);

                            XmlNode searchDirsNode = xmlDocument.SelectSingleNode(OldXmlSettings.SearchDirectoriesPath);
                            if (searchDirsNode != null)
                            {
                                string dirs = searchDirsNode.InnerText.Trim();

                                if (!string.IsNullOrEmpty(dirs))
                                {
                                    List<string> directories = new List<string>();

                                    string[] splitDirs = dirs.Split(new char[] { ',' }, StringSplitOptions.None);

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

                                    if (directories.Count > 0)
                                    {
                                        searchDirectories = new HashSet<string>(directories, StringComparer.OrdinalIgnoreCase);
                                    }
                                }
                            }
                            XmlNode searchSubDirNode = xmlDocument.SelectSingleNode(OldXmlSettings.SearchSubdirectoriesPath);
                            if (searchSubDirNode != null)
                            {
                                bool result;
                                if (bool.TryParse(searchSubDirNode.InnerText.Trim(), out result))
                                {
                                    searchSubdirectories = result;
                                }
                            }

                            changed = true;
                        }
                        else
                        {
                            DataContractSerializer serializer = new DataContractSerializer(typeof(PSFilterPdnSettings));
                            PSFilterPdnSettings settings = (PSFilterPdnSettings)serializer.ReadObject(xmlReader);

                            searchDirectories = settings.searchDirectories;
                            searchSubdirectories = settings.searchSubdirectories;
                        }
                    }
                }
            }
            catch (DirectoryNotFoundException)
            {
                createUserFilesDir = true;
            }
            catch (FileNotFoundException)
            {
                // Use the default settings if the file is not present.
            }
        }

        private void Save()
        {
            if (createUserFilesDir)
            {
                DirectoryInfo info = new DirectoryInfo(Path.GetDirectoryName(path));

                if (!info.Exists)
                {
                    info.Create();
                }
            }

            XmlWriterSettings writerSettings = new XmlWriterSettings
            {
                Indent = true
            };

            using (XmlWriter writer = XmlWriter.Create(path, writerSettings))
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(PSFilterPdnSettings));
                serializer.WriteObject(writer, this);
            }
        }

        private static class OldXmlSettings
        {
            internal const string RootNodeName = "settings";
            internal const string SearchSubdirectoriesPath = "settings/searchSubDirs";
            internal const string SearchDirectoriesPath = "settings/searchDirs";
        }
    }
}
