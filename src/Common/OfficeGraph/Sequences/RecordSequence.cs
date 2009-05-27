﻿using System;
using System.Collections.Generic;
using System.Text;
using DIaLOGIKa.b2xtranslator.StructuredStorage.Reader;

namespace DIaLOGIKa.b2xtranslator.OfficeGraph.Sequences
{
    public class RecordSequence
    {
        IStreamReader _reader;
        public IStreamReader Reader
        {
            get { return _reader; }
            set { this._reader = value; }
        }

        public RecordSequence(IStreamReader reader)
        {
            _reader = reader;
        }

        public OfficeGraphBiffRecord GetNextRecord()
        {
            throw new NotImplementedException();
        }
    }
}
