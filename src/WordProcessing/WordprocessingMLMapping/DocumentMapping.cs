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
using DIaLOGIKa.b2xtranslator.DocFileFormat;
using DIaLOGIKa.b2xtranslator.CommonTranslatorLib;
using System.Xml;
using DIaLOGIKa.b2xtranslator.OpenXmlLib;

namespace DIaLOGIKa.b2xtranslator.WordprocessingMLMapping
{
    public class DocumentMapping : 
        AbstractOpenXmlMapping,
        IMapping<WordDocument>
    {
        private WordDocument _doc;

        public DocumentMapping(XmlWriter writer)
            : base(writer)
        {
        }

        public void Apply(WordDocument doc)
        {
            _doc = doc;

            _writer.WriteStartDocument();
            _writer.WriteStartElement("w", "document", OpenXmlNamespaces.WordprocessingML);
            _writer.WriteStartElement("w", "body", OpenXmlNamespaces.WordprocessingML);

            Int32 cp = 0;
            while (cp < _doc.Text.Count)
            {
                Int32 fc = _doc.PieceTable.FileCharacterPositions[cp];
                ParagraphPropertyExceptions papx = _doc.FindValidPapx(fc);
                TableInfo tai = new TableInfo(papx);

                if (tai.fInTable)
                {
                    //this PAPX is for a table
                    cp = writeTable(cp);
                }
                else
                {
                    //this PAPX is for a normal paragraph
                    cp = writeParagraph(cp);
                }
            }

            _writer.WriteEndElement();
            _writer.WriteEndElement();
            _writer.WriteEndDocument();
        }

        /// <summary>
        /// Writes the table starts at the given cp value
        /// </summary>
        /// <param name="cp">The cp at where the table begins</param>
        /// <returns>The character pointer to the first character after this table</returns>
        private Int32 writeTable(Int32 initialCp)
        {
            Int32 cp = initialCp;
            Int32 fc = _doc.PieceTable.FileCharacterPositions[cp];
            ParagraphPropertyExceptions papx = _doc.FindValidPapx(fc);
            TableInfo tai = new TableInfo(papx);

            //start table
            _writer.WriteStartElement("w", "tbl", OpenXmlNamespaces.WordprocessingML);

            //find the first row end to get and convert the TAPX
            while (!tai.fTtp)
            {
                while (_doc.Text[cp] != TextBoundary.CellOrRowMark)
                {
                    cp++;
                }
                fc = _doc.PieceTable.FileCharacterPositions[cp];
                papx = _doc.FindValidPapx(fc);
                tai = new TableInfo(papx);
                cp++;
            }
            TablePropertyExceptions tapx = new TablePropertyExceptions(papx);
            tapx.Convert(new TablePropertiesMapping(_writer));

            //reset initial values
            cp = initialCp;
            fc = _doc.PieceTable.FileCharacterPositions[cp];
            papx = _doc.FindValidPapx(fc);
            tai = new TableInfo(papx);

            //convert all rows
            while (tai.fInTable)
            {
                cp = writeTableRow(cp);

                fc = _doc.PieceTable.FileCharacterPositions[cp];
                papx = _doc.FindValidPapx(fc);
                tai = new TableInfo(papx);
            }

            //close w:tbl
            _writer.WriteEndElement();

            return cp;
        }

        /// <summary>
        /// Writes the table row that starts at the given cp value and ends at the next row end mark
        /// </summary>
        /// <param name="initialCp">The cp at where the row begins</param>
        /// <returns>The character pointer to the first character after this row</returns>
        private Int32 writeTableRow(Int32 initialCp)
        {
            Int32 cp = initialCp;
            Int32 fc = _doc.PieceTable.FileCharacterPositions[cp];
            ParagraphPropertyExceptions papx = _doc.FindValidPapx(fc);
            TableInfo  tai = new TableInfo(papx);

            //start w:tr
            _writer.WriteStartElement("w", "tr", OpenXmlNamespaces.WordprocessingML);

            while (!(_doc.Text[cp] == TextBoundary.CellOrRowMark && tai.fTtp))
            {
                cp = writeTableCell(cp);

                //each cell has it's own PAPX
                fc = _doc.PieceTable.FileCharacterPositions[cp];
                papx = _doc.FindValidPapx(fc);
                tai = new TableInfo(papx);
            }

            //end w:tr
            _writer.WriteEndElement();

            //skip the row end mark
            cp++;

            return cp;
        }

        /// <summary>
        /// Writes the table cell that starts at the given cp value and ends at the next cell end mark
        /// </summary>
        /// <param name="initialCp">The cp at where the cell begins</param>
        /// <returns>The character pointer to the first character after this cell</returns>
        private Int32 writeTableCell(Int32 initialCp)
        {
            Int32 cp = initialCp;
            Int32 fc = _doc.PieceTable.FileCharacterPositions[cp];

            //start w:tc
            _writer.WriteStartElement("w", "tc", OpenXmlNamespaces.WordprocessingML);

            //find cell end
            Int32 cpCellEnd = initialCp;
            while (_doc.Text[cpCellEnd] != TextBoundary.CellOrRowMark)
            {
                cpCellEnd++;
            }

            //copy the chars of the cell
            List<char> remainingChars = _doc.Text.GetRange(initialCp, cpCellEnd - initialCp);

            //write paragaphs for each paragraph in the cell
            while(remainingChars.Contains('\r'))
            {
                fc = _doc.PieceTable.FileCharacterPositions[cp];
                cp = writeParagraph(cp);
                remainingChars = _doc.Text.GetRange(cp, cpCellEnd - cp);
            }

            //write the remaining chars as own paragraph
            cp = writeParagraph(cp, cpCellEnd);

            //end w:tc
            _writer.WriteEndElement();

            //skip cell end mark
            cp++;

            return cp;
        }

        /// <summary>
        /// Writes a Paragraph that starts at the given cp and 
        /// ends at the next paragraph end mark or section mark
        /// </summary>
        /// <param name="cp"></param>
        private Int32 writeParagraph(Int32 cp) 
        {
            //search the paragraph end
            Int32 cpParaEnd = cp;
            while (_doc.Text[cpParaEnd] != TextBoundary.ParagraphEnd)
            {
                cpParaEnd++;
            }
            cpParaEnd++;

            return writeParagraph(cp, cpParaEnd);
        }

        /// <summary>
        /// Writes a Paragraph that starts at the given cpStart and 
        /// ends at the given cpEnd
        /// </summary>
        /// <param name="cpStart"></param>
        /// <param name="cpEnd"></param>
        /// <returns></returns>
        private Int32 writeParagraph(Int32 cp, Int32 cpEnd)
        {
            Int32 fc = _doc.PieceTable.FileCharacterPositions[cp];
            Int32 fcEnd = _doc.PieceTable.FileCharacterPositions[cpEnd];
            ParagraphPropertyExceptions papx = _doc.FindValidPapx(fc);

            //start paragraph
            _writer.WriteStartElement("w", "p", OpenXmlNamespaces.WordprocessingML);

            //write properties
            papx.Convert(new ParagraphPropertiesMapping(_writer, _doc.Styles));

            //get all CHPX between these boundaries to determine the count of runs
            List<CharacterPropertyExceptions> chpxs = FormattedDiskPageCHPX.GetCharacterPropertyExceptions(
                fc,
                fcEnd,
                _doc.FIB,
                _doc.WordDocumentStream,
                _doc.TableStream);
            List<Int32> chpxFcs = FormattedDiskPageCHPX.GetFileCharacterPositions(
                fc,
                fcEnd,
                _doc.FIB,
                _doc.WordDocumentStream,
                _doc.TableStream);
            chpxFcs.Add(fcEnd);

            //write a rus for each CHPX
            for (int i = 0; i < chpxs.Count; i++)
            {
                //get the chars of this CHPX
                int fcChpxStart = chpxFcs[i];
                int fcChpxEnd = chpxFcs[i + 1];

                //it's the first chpx and it starts before the paragraph
                if (i == 0 && fcChpxStart < fc)
                {
                    //so use the FC of the paragraph
                    fcChpxStart = fc;
                }

                //it's the last chpx and it exceeds the paragraph
                if (i == (chpxs.Count - 1) && fcChpxEnd > fcEnd)
                {
                    //so use the FC of the paragraph
                    fcChpxEnd = fcEnd;
                }

                //read the chars that are formatted via this CHPX
                List<char> chpxChars = _doc.PieceTable.GetChars(fcChpxStart, fcChpxEnd, _doc.WordDocumentStream);

                //write the run
                if (chpxChars.Count > 0)
                    writeRun(chpxChars, chpxs[i]);
            }

            //end paragraph
            _writer.WriteEndElement();

            return cpEnd++;
        }

        /// <summary>
        /// Writes a run with the given characters and CHPX
        /// </summary>
        private void writeRun(List<char> chars, CharacterPropertyExceptions chpx)
        {
            //start run
            _writer.WriteStartElement("w", "r", OpenXmlNamespaces.WordprocessingML);

            //write properties
            chpx.Convert(new CharacterPropertiesMapping(_writer, _doc.Styles, _doc.FontTable));

            //start text
            _writer.WriteStartElement("w", "t", OpenXmlNamespaces.WordprocessingML);

            if ((int)chars[0] == 32 || (int)chars[chars.Count - 1] == 32)
            {
                _writer.WriteAttributeString("xml", "space", "", "preserve");
            }

            //write text
            foreach(char c in chars)
            {
                if (c == TextBoundary.Tab)
                {
                    _writer.WriteElementString("w", "tab", OpenXmlNamespaces.WordprocessingML, "");
                }
                else if (c == TextBoundary.HardLineBreak)
                {
                    _writer.WriteElementString("w", "br", OpenXmlNamespaces.WordprocessingML, "");
                }
                else if(c == TextBoundary.Picture)
                {
                    _writer.WriteString("[PICTURE]");
                }
                else if (c == TextBoundary.ParagraphEnd)
                {
                    //do nothing
                }
                else if (c == TextBoundary.PageBreakOrSectionMark)
                {
                    //do nothing
                }
                else if (c == TextBoundary.ColumnBreak)
                {
                    //do nothing
                }
                else if (c == TextBoundary.FieldBeginMark)
                {
                    _writer.WriteStartElement("w", "fldChar", OpenXmlNamespaces.WordprocessingML);
                    _writer.WriteAttributeString("w", "fldCharType", OpenXmlNamespaces.WordprocessingML, "begin");
                    _writer.WriteEndElement();
                }
                else if (c == TextBoundary.FieldSeperator)
                {
                    _writer.WriteStartElement("w", "fldChar", OpenXmlNamespaces.WordprocessingML);
                    _writer.WriteAttributeString("w", "fldCharType", OpenXmlNamespaces.WordprocessingML, "separate");
                    _writer.WriteEndElement();
                }
                else if (c == TextBoundary.FieldEndMark)
                {
                    _writer.WriteStartElement("w", "fldChar", OpenXmlNamespaces.WordprocessingML);
                    _writer.WriteAttributeString("w", "fldCharType", OpenXmlNamespaces.WordprocessingML, "end");
                    _writer.WriteEndElement();
                }
                else if  ((int)c > 31 && (int)c != 0xFFFF)
                {
                     _writer.WriteString(new String(c, 1));
                }
            }

            //end run
            _writer.WriteEndElement();

            //end run
            _writer.WriteEndElement();
        }
    }
}
