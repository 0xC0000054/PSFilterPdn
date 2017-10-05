/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace PSFilterPdn
{
    [CollectionDataContract(ItemName = "Path", Namespace = "")]
    internal sealed class SearchDirectoryCollection : Collection<string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchDirectoryCollection"/> class.
        /// </summary>
        public SearchDirectoryCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchDirectoryCollection"/> class.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is null.</exception>
        public SearchDirectoryCollection(IList<string> items) : base(items)
        {
        }
    }

    [DataContract(Name = "PSFilterPdnSettings", Namespace = "")]
    [KnownType(typeof(SearchDirectoryCollection))]
    internal sealed class PSFilterPdnSettings
    {
        private readonly string path;
        private bool changed;
        [DataMember(Name = "DirectoryList")]
        private SearchDirectoryCollection searchDirectories;
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
                throw new ArgumentNullException("path");
            }

            this.path = path;
            this.changed = false;
            this.searchSubdirectories = true;
            this.searchDirectories = null;

            try
            {
                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    Load(stream);
                }
            }
            catch (FileNotFoundException)
            {
                // Use the default settings if the file is not present.
            }
        }

        /// <summary>
        /// Gets or sets the search directories.
        /// </summary>
        /// <value>
        /// The search directories.
        /// </value>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null</exception>
        public SearchDirectoryCollection SearchDirectories
        {
            get
            {
                return this.searchDirectories;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                this.searchDirectories = value;
                this.changed = true;
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
            get
            {
                return this.searchSubdirectories;
            }
            set
            {
                if (this.searchSubdirectories != value)
                {
                    this.searchSubdirectories = value;
                    this.changed = true;
                }
            }
        }

        /// <summary>
        /// Saves any changes to this instance.
        /// </summary>
        public void Flush()
        {
            if (this.changed)
            {
                Save();
            }
        }

        private void Load(Stream stream)
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

                    XmlNode searchSubDirNode = xmlDocument.SelectSingleNode(OldXmlSettings.SearchSubdirectoriesPath);
                    if (searchSubDirNode != null)
                    {
                        bool result;
                        if (bool.TryParse(searchSubDirNode.InnerText.Trim(), out result))
                        {
                            this.searchSubdirectories = result;
                        }
                    }
                    XmlNode searchDirsNode = xmlDocument.SelectSingleNode(OldXmlSettings.SearchDirectoriesPath);
                    if (searchDirsNode != null)
                    {
                        string dirs = searchDirsNode.InnerText.Trim();

                        if (!string.IsNullOrEmpty(dirs))
                        {
                            List<string> directories = new List<string>();

                            string[] splitDirs = dirs.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

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
                                this.searchDirectories = new SearchDirectoryCollection(directories);
                            }
                        }
                    }

                    this.changed = true;
                }
                else
                {
                    DataContractSerializer serializer = new DataContractSerializer(typeof(PSFilterPdnSettings));
                    PSFilterPdnSettings settings = (PSFilterPdnSettings)serializer.ReadObject(xmlReader);

                    this.searchDirectories = settings.searchDirectories;
                    this.searchSubdirectories = settings.searchSubdirectories;
                }
            }
        }

        private void Save()
        {
            XmlWriterSettings writerSettings = new XmlWriterSettings
            {
                Indent = true
            };

            using (XmlWriter writer = XmlWriter.Create(this.path, writerSettings))
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
