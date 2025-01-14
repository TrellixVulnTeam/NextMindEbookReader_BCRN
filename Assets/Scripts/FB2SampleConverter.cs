﻿using FB2Library;
using FB2Library.Elements;
using FB2Library.Elements.Poem;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FB2SampleConverter: MonoBehaviour
{
    public interface ILine
    {
    }

    public class HeaderLine : ILine
    {
        public byte HeaderLevel { get; set; }
        public string Text { get; set; }
    }

    public class ImageLine : ILine
    {
        public byte[] Data { get; set; }
    }

    public class TextLine : ILine
    {
        public string Text { get; set; }
    }

    private FB2File _file;

    private List<ILine> _lines = new List<ILine>();

    public virtual async Task<IEnumerable<ILine>> ConvertAsync(FB2File file)
    {
        _lines.Clear();

        return await Task.Factory.StartNew(() =>
        {
            _file = file;

            if (_file.MainBody != null)
            {
                PrepareBodies();
            }

            return _lines;
        });
    }

    protected virtual void PrepareBodies()
    {
        foreach (var bodyItem in _file.Bodies)
        {
            AddTitle(bodyItem.Title);

            foreach (SectionItem sectionItem in bodyItem.Sections)
            {
                PrepareTextItem(sectionItem);
            }
        }
    }

    protected virtual void PrepareTextItems(IEnumerable<IFb2TextItem> textItems)
    {
        foreach (var textItem in textItems)
        {
            if (textItem is IFb2TextItem)
            {
                PrepareTextItem(textItem);
            }
            else
            {
                AddTextLine(textItem.ToString());
            }
        }
    }

    protected virtual void PrepareTextItem(IFb2TextItem textItem)
    {
        if (textItem is CiteItem)
        {
            PrepareTextItems(((CiteItem)textItem).CiteData);
            return;
        }

        if (textItem is PoemItem)
        {
            var item = (PoemItem)textItem;
            AddTitle(item.Title);
            PrepareTextItems(item.Content);
            return;
        }

        if (textItem is SectionItem)
        {
            var item = (SectionItem)textItem;
            AddTitle(item.Title);
            PrepareTextItems(item.Content);
            return;
        }

        if (textItem is StanzaItem)
        {
            var item = (StanzaItem)textItem;
            AddTitle(item.Title);
            PrepareTextItems(item.Lines);
            return;
        }

        if (textItem is ParagraphItem
            || textItem is EmptyLineItem
            || textItem is TitleItem
            || textItem is SimpleText)
        {
            AddTextLine(textItem.ToString());
            return;
        }

        if (textItem is ImageItem)
        {
            var item = (ImageItem)textItem;
            var key = item.HRef.Replace("#", string.Empty);

            if (_file.Images.ContainsKey(key))
            {
                var data = _file.Images[key].BinaryData;
                _lines.Add(new ImageLine { Data = data });
            }
            return;
        }

        if (textItem is DateItem)
        {
            AddTextLine(((DateItem)textItem).DateValue.ToString());
            return;
        }

        if (textItem is EpigraphItem)
        {
            var item = (EpigraphItem)textItem;
            PrepareTextItems(item.EpigraphData);
            return;
        }

        throw new Exception(textItem.GetType().ToString());
    }

    protected virtual void AddTitle(TitleItem titleItem)
    {
        if (titleItem != null)
        {
            foreach (var title in titleItem.TitleData)
            {
                _lines.Add(new HeaderLine { Text = title.ToString() });
            }
        }
    }

    protected virtual void AddTextLine(string text)
    {
        _lines.Add(new TextLine { Text = text });
    }

    public string GetLinesAsText()
    {
        string text = string.Empty;

        for (int i = 0; i < _lines.Count; i++)
        {
            if(_lines[i] is TextLine)
            {
                TextLine t = _lines[i] as TextLine;
                text += t.Text;
            }
        }

        return text;
    }

    public List<Texture2D> GetImages()
    {
        if (_lines == null || _lines.Count == 0)
        {
            return null;
        }

        var textures = new List<Texture2D>();
        var tex = new Texture2D(2, 2);

        for (int i = 0; i < _lines.Count; i++)
        {
            if (_lines[i] is ImageLine)
            {
                ImageLine imageItem = _lines[i] as ImageLine;

                tex.LoadImage(imageItem.Data);

                textures.Add(tex);
            }
        }

        return textures;
    }

    public Texture2D GetImage(int index)
    {
        if (_lines == null || index < 0 || _lines.Count >= index)
        {
            return null;
        }

        Texture2D tex = null;

        if (_lines[index] is ImageLine)
        {
            ImageLine imageItem = _lines[index] as ImageLine;

            tex.LoadImage(imageItem.Data);
        }

        return tex;
    }

    public byte[] GetImageData(int index)
    {
        if (_lines == null || index < 0 || _lines.Count >= index)
        {
            return null;
        }

        byte[] data = null;

        if (_lines[index] is ImageLine)
        {
            ImageLine imageItem = _lines[index] as ImageLine;

            data = imageItem.Data;
        }

        return data;
    }

    public byte[] GetCoverImageData()
    {
        if (_lines == null || _lines.Count == 0)
        {
            return null;
        }

        foreach (var item in _lines)
        {
            if (item is ImageLine)
            {
                ImageLine imageItem = item as ImageLine;

                return imageItem.Data;
            }
        }

        return null;
    }
}
