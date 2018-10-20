using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using GenesisCard.Properties;

namespace GenesisCard {
    public static class Program {
        private const string FrameGoldPath = @"frame\frame_gold.png";
        private const string FrameSilverPath = @"frame\frame_silver.png";
        private const string FrameTohoPath = @"frame\frame_toho.png";
        private const string FrameLegendPath = @"frame\frame_legend.png";
        private const string FrameWhitePath = @"frame\frame_white.png";
        private const string TitleGoldPath = @"frame\title_gold.png";
        private const string TitleSilverPath = @"frame\title_silver.png";
        private const string TitleTohoPath = @"frame\title_toho.png";
        private const string IllustPath = @"frame\illust.png";

        private const int ImageWidth = 656;
        private const int ImageHeight = 994;
        private static readonly int MaxTextWidth = Settings.Default.TitleAlignRight ? 275 : 264;
        private const int IllustTextX = 27;
        private static readonly int TitleTextX = ImageWidth - IllustTextX - MaxTextWidth;
        private const int TextY = 938;
        private const int IdX = 17;
        private const int IdY = 969;
        private const float FontSize = 15.75f;
        private const float IdFontSize = 11.75f;

        private static readonly string TextureFolder = Settings.Default.TextureFolderPath;
        private static readonly string XmlFilePath = Settings.Default.XmlFilePath;
        private static readonly bool TitleAlignRight = Settings.Default.TitleAlignRight;
        private static readonly bool PrintSerial = Settings.Default.PrintSerial;

        private static readonly Bitmap FrameGoldBitmap = new Bitmap(FrameGoldPath);
        private static readonly Bitmap FrameSilverBitmap = new Bitmap(FrameSilverPath);
        private static readonly Bitmap FrameTohoBitmap = new Bitmap(FrameTohoPath);
        private static readonly Bitmap FrameLegendBitmap = new Bitmap(FrameLegendPath);
        private static readonly Bitmap FrameWhiteBitmap = new Bitmap(FrameWhitePath);
        private static readonly Bitmap TitleGoldBitmap = new Bitmap(TitleGoldPath);
        private static readonly Bitmap TitleSilverBitmap = new Bitmap(TitleSilverPath);
        private static readonly Bitmap TitleTohoBitmap = new Bitmap(TitleTohoPath);
        private static readonly Bitmap IllustBitmap = new Bitmap(IllustPath);

        private static readonly Rectangle RawRect = new Rectangle(12, 40, ImageWidth, ImageHeight);
        private static readonly Rectangle FrameRect = new Rectangle(0, 0, ImageWidth, ImageHeight);
        private static readonly Rectangle TitleRect = new Rectangle(14, 833, 630, 148);
        private static readonly Rectangle TitleRectToho = new Rectangle(14, 830, 630, 148);
        private static readonly Rectangle IllustRect = new Rectangle(14, 901, 316, 72);

        private static readonly Font TextFont = new Font(Settings.Default.Font, FontSize * 8, FontStyle.Bold, GraphicsUnit.Pixel);
        private static readonly Font IdTextFont = new Font(Settings.Default.Font, IdFontSize * 8, FontStyle.Bold, GraphicsUnit.Pixel);

        private static readonly char[][] Replacements = {
            new[] {'\u203E', '~'},
            new[] {'\u301C', '～'},
            new[] {'\u49FA', 'ê'},
            new[] {'\u5F5C', 'ū'},
            new[] {'\u66E6', 'à'},
            new[] {'\u66E9', 'è'},
            new[] {'\u8E94', '★'},
            new[] {'\u9A2B', 'á'},
            new[] {'\u9A69', 'Ø'},
            new[] {'\u9A6B', 'ā'},
            new[] {'\u9A6A', 'ō'},
            new[] {'\u9AAD', 'ü'},
            new[] {'\u9B2F', 'ī'},
            new[] {'\u9EF7', 'ē'},
            new[] {'\u9F63', 'Ú'},
            new[] {'\u9F67', 'Ä'},
            new[] {'\u973B', '♠'},
            new[] {'\u9F6A', '♣'},
            new[] {'\u9448', '♦'},
            new[] {'\u9F72', '♥'},
            new[] {'\u9F76', '♡'},
            new[] {'\u9F77', 'é'}
        };

        private static int Main(string[] args) {
            if (args.Length != 0) {
                Console.WriteLine("The application do not need any argument. Please change settings in the config file.");
                return 1;
            }
            var xml = new XmlDocument();
            xml.Load(XmlFilePath);
            var cardDict = new Dictionary<int, Card>();
            foreach (XmlElement card in XmlGetNodes(XmlGetRootElement(xml), "card")) {
                var id = Convert.ToInt32(card.GetAttribute("id"));
                var info = XmlGetNode(card, "info");
                cardDict.Add(id, new Card {
                    Version = Convert.ToByte(XmlGetNodeText(info, "version")),
                    Volume = Volume.GetVolume(Convert.ToByte(XmlGetNodeText(info, "volume_type")), Convert.ToByte(XmlGetNodeText(info, "volume")), Convert.ToByte(XmlGetNodeText(info, "volume_id"))),
                    VolumeId = Convert.ToByte(XmlGetNodeText(info, "volume_id")),
                    Rarity = Convert.ToByte(XmlGetNodeText(info, "rarity")),
                    Texture = XmlGetNodeText(info, "texture"),
                    Title = DoReplacement(XmlGetNodeText(info, "title")),
                    Illustrator = DoReplacement(XmlGetNodeText(info, "illustrator")),
                    Copyright = Convert.ToByte(XmlGetNodeText(info, "copyright")),
                    Year = Convert.ToUInt16(XmlGetNodeText(info, "year")),
                    Frame = Convert.ToByte(XmlGetNodeText(info, "frame")),
                    Bright = Convert.ToInt16(XmlGetNodeText(info, "bright"))
                });
            }
            if (!Directory.Exists(Settings.Default.OutputFolderPath))
                Directory.CreateDirectory(Settings.Default.OutputFolderPath);
            var success = 0;
            foreach (var pair in cardDict) {
                var bitmap = RenderImage(pair.Value, TitleAlignRight, PrintSerial);
                if (bitmap == null) {
                    Console.WriteLine(pair.Value.Texture + ".png -> " + pair.Key + ".png NG!");
                    continue;
                }
                bitmap.Save(Path.Combine(Settings.Default.OutputFolderPath, pair.Key + ".png"), ImageFormat.Png);
                Console.WriteLine(pair.Value.Texture + ".png -> " + pair.Key + ".png OK!");
                success++;
            }
            Console.WriteLine("All done. " + success + " file" + (success > 1 ? "s" : "") + " printed.");
            Console.ReadKey();
            return 0;
        }

        private static Bitmap RenderImage(Card card, bool renderTitleFromRight, bool renderId) {
            var path = Path.Combine(TextureFolder, card.Texture + ".png");
            if (!File.Exists(path))
                return null;
            var layerImage = new Bitmap(path);
            var g = CreateGraphicFromBitmap(layerImage);
            var title = false;
            var titleToho = false;
            Bitmap layerInfo = null;
            Bitmap layerFrame = null;
            Bitmap layerTextIllust = null;
            Bitmap layerTextTitle = null;
            switch (card.Frame) {
                case 1:
                    title = true;
                    layerInfo = card.Rarity > 3 ? TitleGoldBitmap : TitleSilverBitmap;
                    layerFrame = card.Rarity > 3 ? FrameGoldBitmap : FrameSilverBitmap;
                    layerTextIllust = CreateBitmap(g.MeasureString(card.Illustrator, TextFont), 8);
                    g.Dispose();
                    g = CreateGraphicFromBitmap(layerTextIllust);
                    g.DrawString(card.Illustrator, TextFont, Brushes.White, 0, 0, StringFormat.GenericTypographic);
                    g.DrawString(card.Illustrator, TextFont, Brushes.Black, 8, 0, StringFormat.GenericTypographic);
                    layerTextTitle = CreateBitmap(g.MeasureString(card.Title, TextFont), 8);
                    g.Dispose();
                    g = CreateGraphicFromBitmap(layerTextTitle);
                    g.DrawString(card.Title, TextFont, Brushes.White, 0, 0, StringFormat.GenericTypographic);
                    g.DrawString(card.Title, TextFont, Brushes.Black, 8, 0, StringFormat.GenericTypographic);
                    break;
                case 2:
                    layerInfo = IllustBitmap;
                    layerFrame = card.Rarity > 3 ? FrameGoldBitmap : FrameSilverBitmap;
                    layerTextIllust = CreateBitmap(g.MeasureString(card.Illustrator, TextFont), 8);
                    g.Dispose();
                    g = CreateGraphicFromBitmap(layerTextIllust);
                    g.DrawString(card.Illustrator, TextFont, Brushes.White, 0, 0, StringFormat.GenericTypographic);
                    g.DrawString(card.Illustrator, TextFont, Brushes.Black, 8, 0, StringFormat.GenericTypographic);
                    break;
                case 3:
                    layerTextIllust = CreateBitmap(g.MeasureString(card.Illustrator, TextFont), 8);
                    g.Dispose();
                    g = CreateGraphicFromBitmap(layerTextIllust);
                    g.DrawString(card.Illustrator, TextFont, Brushes.White, 0, 0, StringFormat.GenericTypographic);
                    g.DrawString(card.Illustrator, TextFont, Brushes.Black, 8, 0, StringFormat.GenericTypographic);
                    break;
                case 4:
                    break;
                case 5:
                    layerFrame = FrameLegendBitmap;
                    layerTextIllust = CreateBitmap(g.MeasureString(card.Illustrator, TextFont));
                    g.Dispose();
                    g = CreateGraphicFromBitmap(layerTextIllust);
                    g.DrawString(card.Illustrator, TextFont, Brushes.White, 0, 0, StringFormat.GenericTypographic);
                    layerTextTitle = CreateBitmap(g.MeasureString(card.Title, TextFont));
                    g.Dispose();
                    g = CreateGraphicFromBitmap(layerTextTitle);
                    g.DrawString(card.Title, TextFont, Brushes.White, 0, 0, StringFormat.GenericTypographic);
                    break;
                case 6:
                    title = true;
                    titleToho = true;
                    layerInfo = TitleTohoBitmap;
                    layerFrame = FrameTohoBitmap;
                    layerTextIllust = CreateBitmap(g.MeasureString(card.Illustrator, TextFont), 8);
                    g.Dispose();
                    g = CreateGraphicFromBitmap(layerTextIllust);
                    g.DrawString(card.Illustrator, TextFont, Brushes.White, 0, 0, StringFormat.GenericTypographic);
                    g.DrawString(card.Illustrator, TextFont, Brushes.Black, 8, 0, StringFormat.GenericTypographic);
                    layerTextTitle = CreateBitmap(g.MeasureString(card.Title, TextFont), 8);
                    g.Dispose();
                    g = CreateGraphicFromBitmap(layerTextTitle);
                    g.DrawString(card.Title, TextFont, Brushes.White, 0, 0, StringFormat.GenericTypographic);
                    g.DrawString(card.Title, TextFont, Brushes.Black, 8, 0, StringFormat.GenericTypographic);
                    break;
                case 7:
                    layerInfo = IllustBitmap;
                    layerFrame = FrameTohoBitmap;
                    layerTextIllust = CreateBitmap(g.MeasureString(card.Illustrator, TextFont), 8);
                    g.Dispose();
                    g = CreateGraphicFromBitmap(layerTextIllust);
                    g.DrawString(card.Illustrator, TextFont, Brushes.White, 0, 0, StringFormat.GenericTypographic);
                    g.DrawString(card.Illustrator, TextFont, Brushes.Black, 8, 0, StringFormat.GenericTypographic);
                    break;
                case 10:
                    title = true;
                    layerInfo = TitleGoldBitmap;
                    layerFrame = FrameGoldBitmap;
                    layerTextIllust = CreateBitmap(g.MeasureString(card.Illustrator, TextFont), 8);
                    g.Dispose();
                    g = CreateGraphicFromBitmap(layerTextIllust);
                    g.DrawString(card.Illustrator, TextFont, Brushes.White, 0, 0, StringFormat.GenericTypographic);
                    g.DrawString(card.Illustrator, TextFont, Brushes.Black, 8, 0, StringFormat.GenericTypographic);
                    layerTextTitle = CreateBitmap(g.MeasureString(card.Title, TextFont), 8);
                    g.Dispose();
                    g = CreateGraphicFromBitmap(layerTextTitle);
                    g.DrawString(card.Title, TextFont, Brushes.White, 0, 0, StringFormat.GenericTypographic);
                    g.DrawString(card.Title, TextFont, Brushes.Black, 8, 0, StringFormat.GenericTypographic);
                    break;
                case 11:
                    layerInfo = IllustBitmap;
                    layerFrame = FrameGoldBitmap;
                    layerTextIllust = CreateBitmap(g.MeasureString(card.Illustrator, TextFont), 8);
                    g.Dispose();
                    g = CreateGraphicFromBitmap(layerTextIllust);
                    g.DrawString(card.Illustrator, TextFont, Brushes.White, 0, 0, StringFormat.GenericTypographic);
                    g.DrawString(card.Illustrator, TextFont, Brushes.Black, 8, 0, StringFormat.GenericTypographic);
                    break;
                case 12:
                    layerFrame = FrameWhiteBitmap;
                    break;
                case 13:
                    layerFrame = FrameSilverBitmap;
                    break;
                default:
                    return null;
            }
            var result = CreateBitmap(new SizeF(ImageWidth, ImageHeight));
            g.Dispose();
            g = CreateGraphicFromBitmap(result);
            g.DrawImage(layerImage, FrameRect, RawRect, GraphicsUnit.Pixel);
            if (layerInfo != null)
                g.DrawImage(layerInfo, title ? (titleToho ? TitleRectToho : TitleRect) : IllustRect);
            if (layerFrame != null)
                g.DrawImage(layerFrame, FrameRect);
            if (layerTextIllust != null) {
                if (layerTextIllust.Width > MaxTextWidth * 8)
                    g.DrawImage(layerTextIllust, IllustTextX, TextY, MaxTextWidth, layerTextIllust.Height / 8.0f);
                else
                    g.DrawImage(layerTextIllust, IllustTextX, TextY, layerTextIllust.Width / 8.0f, layerTextIllust.Height / 8.0f);
            }
            if (layerTextTitle != null) {
                if (layerTextTitle.Width > MaxTextWidth * 8)
                    g.DrawImage(layerTextTitle, TitleTextX, TextY, MaxTextWidth, layerTextTitle.Height / 8.0f);
                else if (renderTitleFromRight)
                    g.DrawImage(layerTextTitle, CalcTitleTextX(layerTextTitle.Width / 8.0f), TextY, layerTextTitle.Width / 8.0f, layerTextTitle.Height / 8.0f);
                else
                    g.DrawImage(layerTextTitle, TitleTextX, TextY, layerTextTitle.Width / 8.0f, layerTextTitle.Height / 8.0f);
            }
            if (renderId && card.Volume.Type < 2) {
                var id = card.GetId();
                var idBitmap = CreateBitmap(g.MeasureString(id, IdTextFont));
                var idG = CreateGraphicFromBitmap(idBitmap);
                idG.DrawString(id, IdTextFont, Brushes.Black, 0, 0, StringFormat.GenericTypographic);
                g.DrawImage(idBitmap, IdX, IdY, idBitmap.Width / 8.0f, idBitmap.Height / 8.0f);
                idG.Dispose();
                idBitmap.Dispose();
            }
            g.Dispose();
            layerImage.Dispose();
            layerTextIllust?.Dispose();
            layerTextTitle?.Dispose();
            return result;
        }

        private static Graphics CreateGraphicFromBitmap(Bitmap bitmap) {
            var g = Graphics.FromImage(bitmap);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            return g;
        }

        private static Bitmap CreateBitmap(SizeF size) {
            return new Bitmap((int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height));
        }

        private static Bitmap CreateBitmap(SizeF size, int x) {
            return new Bitmap((int)Math.Ceiling(size.Width + x), (int)Math.Ceiling(size.Height));
        }

        private static float CalcTitleTextX(float width) {
            return TitleTextX + MaxTextWidth - width;
        }

        private static string DoReplacement(string input) {
            return Replacements.Aggregate(input, (current, replacement) => current.Replace(replacement[0], replacement[1]));
        }

        private static XmlElement XmlGetRootElement(XmlDocument doc) {
            if (doc == null)
                throw new XmlException();
            var root = doc.DocumentElement;
            if (root == null)
                throw new XmlException();
            return root;
        }

        private static XmlNodeList XmlGetNodes(XmlNode node, string name) {
            var list = node.SelectNodes(name);
            if (list == null)
                throw new XmlException();
            return list;
        }

        private static XmlNode XmlGetNode(XmlNode node, string name) {
            var child = node.SelectSingleNode(name);
            if (child == null)
                throw new XmlException();
            return child;
        }

        private static string XmlGetNodeText(XmlNode node, string name) {
            return XmlGetNode(node, name).InnerText;
        }
    }
}