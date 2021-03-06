﻿using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace VsExplorer.Implementation.TreeView
{
    internal sealed class TreeViewController : ITreeViewHost
    {
        private readonly ITextDocumentFactoryService _textDocumentFactoryService;
        private readonly ITextBufferDisplayHost _textBufferDisplayHost;
        private readonly List<SourceBufferInfo> _sourceBufferInfoCollection = new List<SourceBufferInfo>();
        private ITextView _textView;
        private TreeViewDisplay _treeViewDisplay;

        internal TreeViewController(ITextDocumentFactoryService textDocumentFactoryService, ITextBufferDisplayHost textBufferDisplayHost)
        {
            _textDocumentFactoryService = textDocumentFactoryService;
            _textBufferDisplayHost = textBufferDisplayHost;
            _treeViewDisplay = new TreeViewDisplay();
            _treeViewDisplay.TextBufferControl = _textBufferDisplayHost.Visual;
            _treeViewDisplay.SelectedSourceBufferInfoChanged += OnSelectedSourceBufferInfoChanged;
        }

        private void UpdateDisplay()
        {
            _treeViewDisplay.NamedBufferInfoCollection.Clear();
            _sourceBufferInfoCollection.ForEach(x => x.Close());
            _sourceBufferInfoCollection.Clear();

            if (_textView == null)
            {
                return;
            }

            var map = GetSourceBufferInfoMap(_textView);

            Action<string, ITextBuffer> addOne = (name, textBuffer) =>
            {
                var sourceBufferInfo = map[textBuffer];
                var namedBufferInfo = new NamedBufferInfo()
                {
                    Name = name,
                    SourceBufferInfo = sourceBufferInfo
                };

                _treeViewDisplay.NamedBufferInfoCollection.Add(namedBufferInfo);
            };

            addOne("Document Buffer", _textView.TextViewModel.DataModel.DocumentBuffer);
            addOne("Data Buffer", _textView.TextViewModel.DataBuffer);
            addOne("Edit Buffer", _textView.TextViewModel.EditBuffer);
            addOne("Visual Buffer", _textView.TextViewModel.VisualBuffer);

            _sourceBufferInfoCollection.AddRange(map.Values);
            _textBufferDisplayHost.TextBuffer = _textView.TextBuffer;
        }

        private Dictionary<ITextBuffer, SourceBufferInfo> GetSourceBufferInfoMap(ITextView textView)
        {
            var buffers = VsUtil.GetTextBuffersRecursive(textView);
            var idSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var map = new Dictionary<ITextBuffer, SourceBufferInfo>();

            int generator = 1;
            foreach (var textBuffer in buffers)
            {
                string documentPath = string.Empty;
                string id = null;
                ITextDocument textDocument;
                if (_textDocumentFactoryService.TryGetTextDocument(textBuffer, out textDocument))
                {
                    documentPath = textDocument.FilePath;
                    var fileName = string.Format("{{{0}}}", Path.GetFileName(documentPath));
                    if (!idSet.Contains(fileName))
                    {
                        id = fileName;
                    }
                }

                if (id == null)
                {
                    do
                    {
                        id = string.Format("{{Unnamed {0}}}", generator.ToString());
                        generator++;
                    } while (idSet.Contains(id));
                }

                var sourceBufferInfo = new SourceBufferInfo(textBuffer, id, documentPath);
                map.Add(textBuffer, sourceBufferInfo);
            }

            UpdateChildren(map);
            return map;
        }

        private void UpdateChildren(Dictionary<ITextBuffer, SourceBufferInfo> map)
        {
            foreach (var sourceBufferInfo in map.Values)
            {
                var projectionBuffer = sourceBufferInfo.TextBuffer as IProjectionBufferBase;
                if (projectionBuffer == null)
                {
                    continue;
                }

                foreach (var textBuffer in projectionBuffer.SourceBuffers)
                {
                    sourceBufferInfo.Children.Add(map[textBuffer]);
                }
            }
        }

        private void OnSelectedSourceBufferInfoChanged(object sender, SourceBufferInfoEventArgs e)
        {
            _textBufferDisplayHost.TextBuffer = e.SourceBufferInfo.TextBuffer;
        }

        #region ITreeViewHost

        UIElement ITreeViewHost.Visual
        {
            get { return _treeViewDisplay; }
        }

        ITextView ITreeViewHost.TextView
        {
            get { return _textView; }
            set
            {
                if (_textView != value)
                {
                    _textView = value;
                    UpdateDisplay();
                }
            }
        }

        #endregion
    }
}
