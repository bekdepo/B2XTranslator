/*
 * Copyright (c) 2008, DIaLOGIKa
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *        notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of DIaLOGIKa nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY DIaLOGIKa ''AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL DIaLOGIKa BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */


using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using DIaLOGIKa.b2xtranslator.StructuredStorage.Common;
using DIaLOGIKa.b2xtranslator.StructuredStorage.Reader;

namespace DIaLOGIKa.b2xtranslator.StructuredStorage.Writer
{
    public class StructuredStorageWriter
    {
        StructuredStorageContext _context;


        public StorageDirectoryEntry RootDirectoryEntry
        {
            get { return _context.RootDirectoryEntry; }
        }
        

        public StructuredStorageWriter()
        {
            _context = new StructuredStorageContext();
        }


        public void write(Stream outputStream)
        {
            _context.RootDirectoryEntry.RecursiveCreateRedBlackTrees();
            List<BaseDirectoryEntry> allEntries = _context.RootDirectoryEntry.RecursiveGetAllDirectoryEntries();
            allEntries.Sort(
                    delegate(BaseDirectoryEntry a, BaseDirectoryEntry b)
                    { return a.Sid.CompareTo(b.Sid); }
                );


            // write Streams
            foreach (BaseDirectoryEntry entry in allEntries)
            {
                if (entry.Sid == 0x0)
                {
                    // root entry
                    continue;
                }

                entry.writeReferencedStream();
            }

            // root entry has to be written after all other streams as it contains the ministream to which other _entries write to
            _context.RootDirectoryEntry.writeReferencedStream();

            // write Directory Entries to directory stream
            foreach (BaseDirectoryEntry entry in allEntries)
            {
                entry.write();
            }

            // write directory stream
            VirtualStream virtualDirectoryStream = new VirtualStream(_context.DirectoryStream.BaseStream, _context.Fat, _context.Header.SectorSize, _context.TempOutputStream);
            virtualDirectoryStream.write();
            _context.Header.DirectoryStartSector = virtualDirectoryStream.StartSector;
            if (_context.Header.SectorSize == 0x1000)
            {
                _context.Header.NoSectorsInDirectoryChain4KB = (UInt32)virtualDirectoryStream.SectorCount;
            }
            
            // write MiniFat
            _context.MiniFat.write();
            _context.Header.MiniFatStartSector = _context.MiniFat.MiniFatStart;
            _context.Header.NoSectorsInMiniFatChain = _context.MiniFat.NumMiniFatSectors;

            _context.Fat.write();

            _context.Header.NoSectorsInDiFatChain = _context.Fat.NumDiFatSectors;
            _context.Header.NoSectorsInFatChain = _context.Fat.NumFatSectors;
            _context.Header.DiFatStartSector = _context.Fat.DiFatStartSector;
            
            _context.Header.write();

            _context.Header.writeToStream(outputStream);
            _context.TempOutputStream.writeToStream(outputStream);
        }
    }
}