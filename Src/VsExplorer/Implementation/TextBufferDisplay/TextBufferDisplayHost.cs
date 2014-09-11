﻿using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace VsExplorer.Implementation.TextBufferDisplay
{
    internal sealed class TextBufferDisplayHost : ITextBufferDisplayHost
    {
        private readonly ITextDocumentFactoryService _textDocumentFactoryService;
        private readonly TextBufferDisplayControl _textBufferDisplayControl;
        private ITextBuffer _textBuffer;

        internal TextBufferDisplayHost(ITextDocumentFactoryService textDocumentFactoryService)
        {
            _textDocumentFactoryService = textDocumentFactoryService;
            _textBufferDisplayControl = new TextBufferDisplayControl();
        }

        private void UpdateTextBuffer(ITextBuffer textBuffer)
        {
            if (_textBuffer != null)
            {
                WeakEventManager<ITextBuffer, TextContentChangedEventArgs>.RemoveHandler(_textBuffer, "Changed", OnTextBufferChanged);
            }

            _textBuffer = textBuffer;

            if (textBuffer != null)
            {
                _textBufferDisplayControl.TextBufferInfo = CreateTextBufferInfo(textBuffer);
                WeakEventManager<ITextBuffer, TextContentChangedEventArgs>.AddHandler(_textBuffer, "Changed", OnTextBufferChanged);
            }
            else
            {
                _textBufferDisplayControl.TextBufferInfo = TextBufferInfo.Empty;
            }
        }

        private TextBufferInfo CreateTextBufferInfo(ITextBuffer textBuffer)
        {
            string name = null;
            string documentPath = null;
            ITextDocument textDocument;
            if (_textDocumentFactoryService.TryGetTextDocument(textBuffer, out textDocument))
            {
                documentPath = textDocument.FilePath;
                name = Path.GetFileName(documentPath);
            }
            else 
            {
                // TODO: Get a better predictable name here 
                name = "Unnamed Buffer";
            }

            var textBufferInfo = new TextBufferInfo(
                name,
                documentPath,
                textBuffer.ContentType.TypeName,
                textBuffer.CurrentSnapshot.GetText());

            AddSourceBuffers(textBuffer, textBufferInfo);
            return textBufferInfo;
        }

        private void AddSourceBuffers(ITextBuffer textBuffer, TextBufferInfo textBufferInfo)
        {
            var projectionBuffer = textBuffer as IProjectionBufferBase;
            if (projectionBuffer == null)
            {
                return;
            }

            foreach (var sourceBuffer in projectionBuffer.SourceBuffers)
            {
                textBufferInfo.SourceBuffers.Add(CreateTextBufferInfo(sourceBuffer));
            }
        }

        private void OnTextBufferChanged(object sender, EventArgs e)
        {
            _textBufferDisplayControl.TextBufferInfo.Text = _textBuffer.CurrentSnapshot.GetText();
        }

        #region ITextBufferDisplayHost

        UIElement ITextBufferDisplayHost.Visual
        {
            get { return _textBufferDisplayControl; }
        }

        ITextBuffer ITextBufferDisplayHost.TextBuffer
        {
            get { return _textBuffer; }
            set { UpdateTextBuffer(value); }
        }

        #endregion
    }
}