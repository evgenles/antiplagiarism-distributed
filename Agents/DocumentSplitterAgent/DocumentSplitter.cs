using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Agent.Abstract;
using Agent.Abstract.Models;
using DocumentFormat.OpenXml;
using Transport.Abstraction;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocumentSplitterAgent
{
    
    /// <summary>
    /// Document splitter take one big document to input and split it to a lot of documents by heading 1(and based
    /// on heading 1) styles.
    /// He know docx structure
    /// He can separate text and style from images and table and split by heading 1 (and based on) styles
    /// </summary>
    public class DocumentSplitter : AgentAbstract
    {
        public DocumentSplitter(ITransportSender transport) : base(transport, AgentType.Splitter, "",
            MessageType.Unknown, MessageType.SplitterTask)
        {
        }

        public override async Task ProcessMessageAsync(AgentMessage message)
        {
            var task = message.Data.ToObject<TaskMessage>();
            using var dataStream = new MemoryStream(task.Data);
            var document = WordprocessingDocument.Open(dataStream, false);
            XDocument styles = ExtractStylesPart(document);
            List<string> headingBasedStyles = GetHeadingStylesId(document);

            var resultStreams = new List<(string name, Stream data)>();
            var stream = new MemoryStream();
            var (currentDoc, currBody) = CreteNewDocToStream(stream, styles);
            var name = "Титульные страницы";
            foreach (var sourceParagraph in document.MainDocumentPart.Document.Body.Elements<Paragraph>().ToList())
            {
                var destParagraph = new Paragraph();
                CopyParagraphProperties(sourceParagraph, destParagraph);
                CopyOnlyParagraphText(sourceParagraph, destParagraph);
                
                //Separete into new stream by heading based styles paragraph
                if (headingBasedStyles.Contains(sourceParagraph.ParagraphProperties?.Elements<ParagraphStyleId>()
                    .FirstOrDefault()
                    ?.Val?.Value))
                {
                    currentDoc.Close();
                    stream.Seek(0, SeekOrigin.Begin);
                    resultStreams.Add((name, stream));
                    stream = new MemoryStream();
                    name = destParagraph.InnerText.Substring(0, Math.Min(100, destParagraph.InnerText.Length));
                    (currentDoc, currBody) = CreteNewDocToStream(stream, styles);
                }

               

                currBody.AppendChild(destParagraph);
            }

            currentDoc.Close();
            stream.Seek(0, SeekOrigin.Begin);
            resultStreams.Add((name,stream));
            
            for (int i = 0; i < resultStreams.Count; i++)
            {
                if (Transport != null)
                {
                    await using var ms = new MemoryStream();
                    await resultStreams[i].data.CopyToAsync(ms);
                    await Transport.SendAsync(MessageType.WorkerTask.ToString(), new AgentMessage<TaskMessage>
                    {
                        Author = this,
                        MessageType = MessageType.WorkerTask,
                        Data = new TaskMessage
                        {
                            Creator = task.Creator,
                            ParentId = task.Id,
                            Name = $"{task.Name}->{resultStreams[i].name}",
                            Data = ms.ToArray(),
                            RequiredSubtype = task.RequiredSubtype
                        }
                    });
                }

                // using var outStream = File.Create($"{i}_{resultStreams[i].name}.docx");
                // resultStreams[i].data.CopyTo(outStream);
            }
        }

        private (WordprocessingDocument doc, Body body) CreteNewDocToStream(Stream stream, XDocument styles)
        {
            var newDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
            var newMainPart = newDoc.AddMainDocumentPart();
            newMainPart.Document = new Document();
            SetStyleToTarget(newDoc, styles);
            var body = newMainPart.Document.AppendChild(new Body());
            return (newDoc, body);
        }

        private List<string> GetHeadingStylesId(WordprocessingDocument document)
        {
            var allDocumentStyles = document
                .MainDocumentPart
                .StyleDefinitionsPart.Styles.Elements<Style>()
                .ToList();
            var head1 = allDocumentStyles
                .FirstOrDefault(x =>
                    x.StyleName.Val.Value.Equals("heading 1", StringComparison.InvariantCultureIgnoreCase));
            List<string> headingBasedStyles = new List<string>();
            if (head1 != null)
            {
                headingBasedStyles = allDocumentStyles.Where(x => x.BasedOn?.Val?.Value == head1.StyleId)
                    .Select(x => x.StyleId.Value)
                    .ToList();
                headingBasedStyles.Add(head1.StyleId.Value);
            }

            return headingBasedStyles;
        }

        private XDocument ExtractStylesPart(WordprocessingDocument doc)
        {
            XDocument styles = null;

            var docPart = doc.MainDocumentPart;

            StylesPart stylesPart = docPart.StyleDefinitionsPart;
            if (stylesPart != null)
            {
                using var reader = XmlReader.Create(stylesPart.GetStream(FileMode.Open, FileAccess.Read));
                // Create the XDocument.
                styles = XDocument.Load(reader);
            }

            return styles;
        }

        private void SetStyleToTarget(WordprocessingDocument doc, XDocument newStyles)
        {
            // add or get the style definition part
            StyleDefinitionsPart styleDefn = null;
            if (doc.MainDocumentPart.StyleDefinitionsPart == null)
            {
                styleDefn = doc.MainDocumentPart.AddNewPart<StyleDefinitionsPart>();
            }
            else
            {
                styleDefn = doc.MainDocumentPart.StyleDefinitionsPart;
            }

            // populate part with the new styles
            if (styleDefn != null)
            {
                // write the newStyle xDoc to the StyleDefinitionsPart using a streamwriter
                newStyles.Save(new StreamWriter(
                    styleDefn.GetStream(FileMode.Create, FileAccess.Write)));
            }
        }

        private void CopyParagraphProperties(Paragraph source, Paragraph dest)
        {
            if (source.ParagraphProperties != null)
            {
                dest.ParagraphProperties = new ParagraphProperties();
                foreach (var element in source.ParagraphProperties.Elements())
                {
                    if (!(element is SectionProperties))
                    {
                        dest.ParagraphProperties.AppendChild((OpenXmlElement) element.Clone());
                    }
                }
            }
        }

        private void CopyOnlyParagraphText(Paragraph source, Paragraph dest)
        {
            var runs = source.Elements<Run>().ToList();
            foreach (var inRun in runs)
            {
                var texts = inRun.Elements<Text>().ToList();
                if (texts.Any())
                {
                    var run = new Run();
                    texts.ForEach(t => run.AppendChild((Text) t.Clone()));
                    dest.AppendChild(run);
                }
            }
        }

        public override Task<AgentMessage> ProcessRpcAsync(AgentMessage<RpcRequest> message)
        {
            return Task.FromResult<AgentMessage>(null);
        }
    }
}