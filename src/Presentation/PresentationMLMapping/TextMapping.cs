using System;
using System.Collections.Generic;
using System.Text;
using DIaLOGIKa.b2xtranslator.PptFileFormat;
using DIaLOGIKa.b2xtranslator.CommonTranslatorLib;
using System.Xml;
using DIaLOGIKa.b2xtranslator.OpenXmlLib;
using DIaLOGIKa.b2xtranslator.OfficeDrawing;

namespace DIaLOGIKa.b2xtranslator.PresentationMLMapping
{
    class TextMapping :
        AbstractOpenXmlMapping,
        IMapping<ClientTextbox>
    {
        protected ConversionContext _ctx;

        public TextMapping(ConversionContext ctx, XmlWriter writer)
            : base(writer)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// Returns the ParagraphRun of the given style that is active at the given index.
        /// </summary>
        /// <param name="style">style to use</param>
        /// <param name="forIdx">index to use</param>
        /// <returns>ParagraphRun or null in case no run was found</returns>
        protected static ParagraphRun GetParagraphRun(StyleTextPropAtom style, uint forIdx)
        {
            if (style == null)
                return null;

            uint idx = 0;

            foreach (ParagraphRun p in style.PRuns)
            {
                if (forIdx <= idx)
                    return p;

                idx += p.Length;
            }

            return null;
        }

        /// <summary>
        /// Returns the CharacterRun of the given style that is active at the given index.
        /// </summary>
        /// <param name="style">style to use</param>
        /// <param name="forIdx">index to use</param>
        /// <returns>CharacterRun or null in case no run was found</returns>
        protected static CharacterRun GetCharacterRun(StyleTextPropAtom style, uint forIdx)
        {
            if (style == null)
                return null;

            uint idx = 0;

            foreach (CharacterRun c in style.CRuns)
            {
                if (forIdx <= idx)
                    return c;

                idx += c.Length;
            }

            return null;
        }

        public void Apply(ClientTextbox textbox)
        {
            TextAtom textAtom = textbox.FirstChildWithType<TextAtom>();
            string text = (textAtom == null) ? "" : textAtom.Text;

            StyleTextPropAtom style = textbox.FirstChildWithType<StyleTextPropAtom>();

            Console.WriteLine("TextMapping: text = {0}", Tools.Utils.StringInspect(text));

            uint idx = 0;

            // Special case: always write out at least one paragraph (even if idx == text.length == 0)
            while (idx < text.Length || text.Length == 0)
            {
                ParagraphRun p = GetParagraphRun(style, idx);

                uint pEndIdx = (p != null) ? (uint)Math.Min(idx + p.Length, text.Length) : (uint)text.Length;

                Console.WriteLine("Paragraph run from {0} to {1}", idx, pEndIdx);

                _writer.WriteStartElement("a", "p", OpenXmlNamespaces.DrawingML);

                while (idx < pEndIdx)
                {
                    CharacterRun r = GetCharacterRun(style, idx);
                    // Current run length or remaining text length if no run available
                    uint rlen = (r != null) ? r.Length : (uint)(text.Length - idx);

                    // Remaining paragraph length
                    uint plen = pEndIdx - idx;

                    // Remaining text length
                    uint tlen = (uint)(text.Length - idx);

                    // Length of extracted subtext can't go beyond character run,
                    // remaining paragraph run and remaining text length so limit it.
                    uint slen = rlen;
                    if (slen > tlen)
                        slen = tlen;
                    if (slen > plen)
                        slen = plen;

                    String subtext = text.Substring((int)idx, (int)slen).Trim(new char[] {'\r', '\n', '\v'});

                    Console.WriteLine("Character run from {0} to {1} ({3}): {2}",
                        idx, idx + rlen, Tools.Utils.StringInspect(subtext), slen);

                    if (subtext.Length > 0)
                    {
                        _writer.WriteStartElement("a", "r", OpenXmlNamespaces.DrawingML);
                        /*if (r != null)
                            new CharacterRunPropsMapping(_ctx, _writer).Apply(r);*/

                        _writer.WriteStartElement("a", "t", OpenXmlNamespaces.DrawingML);
                        _writer.WriteValue(subtext);
                        _writer.WriteEndElement();

                        _writer.WriteEndElement();
                    }

                    idx += rlen;
                }

                _writer.WriteStartElement("a", "endParaRPr", OpenXmlNamespaces.DrawingML);
                // TODO...
                _writer.WriteEndElement();

                _writer.WriteEndElement();

                idx = pEndIdx;

                /* Didn't move so stop looping */
                if (text.Length == 0)
                    break;
            }
        }
    }
}