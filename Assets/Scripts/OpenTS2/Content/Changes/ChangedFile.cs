﻿using OpenTS2.Common;
using OpenTS2.Files.Formats.DBPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTS2.Content.Changes
{
    public class ChangedFile : AbstractChanged
    {
        public bool compressed = false;
        protected byte[] fileData;
        public ChangedFile(byte[] fileData, ResourceKey tgi, DBPFFile package, AbstractCodec codec = null)
        {
            this.fileData = fileData;
            if (codec != null)
            {
                this.asset = codec.Deserialize(fileData, tgi, package);
            }
        }
        public ChangedFile(AbstractAsset asset, byte[] fileData) : this(fileData, asset.internalTGI, asset.package)
        {

        }
        public override byte[] bytes
        {
            get
            {
                return fileData;
            }
            set
            {
                fileData = value;
            }
        }
    }
}
