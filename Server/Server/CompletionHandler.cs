using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    internal class CompletionHandler : ICompletionHandler
    {
        private const string PackageReferenceElement = "PackageReference";
        private const string IncludeAttribute = "Include";
        private const string VersionAttribute = "Version";
        private static readonly char[] EndElement = new[] { '>' };

        private readonly ILanguageServer _router;
        private readonly BufferManager _bufferManager;

        private readonly DocumentSelector _documentSelector = new DocumentSelector(
            new DocumentFilter()
            {
                Pattern = "**/*.csproj"
            }
        );

        private CompletionCapability _capability;

        public CompletionHandler(ILanguageServer router, BufferManager bufferManager)
        {
            _router = router;
            _bufferManager = bufferManager;
        }

        public CompletionRegistrationOptions GetRegistrationOptions()
        {
            return new CompletionRegistrationOptions
            {
                DocumentSelector = _documentSelector,
                ResolveProvider = false
            };
        }

        public async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            var documentPath = request.TextDocument.Uri.ToString();
            var buffer = _bufferManager.GetBuffer(documentPath);

            if (string.IsNullOrEmpty(buffer))
            {
                return new CompletionList();
            }

            var wordToComplete = GetWordToComplete(request, buffer);

            if (!string.IsNullOrWhiteSpace(wordToComplete))
            {
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetStringAsync($"https://api-v2v3search-0.nuget.org/autocomplete?q={wordToComplete}");
                    var items = JObject.Parse(response)["data"].ToObject<List<string>>().Select(x => new CompletionItem
                    {
                        Label = x,
                    }).ToArray();

                    return new CompletionList(items.Length > 1, items);
                }
            }

            return new CompletionList();
        }

        private string GetWordToComplete(CompletionParams request, string buffer)
        {
            var line = (int)request.Position.Line;
            var col = (int)request.Position.Character;
            var bufferSpan = buffer.AsSpan();

            // Find the position
            var index = 0;
            for (var i = 0; i < line; i++)
            {
                index = buffer.IndexOf('\n', index) + 1;
            }
            index += col;

            // Find the last < char before position
            var pos = index;
            var elementStart = bufferSpan
                .Slice(0, pos)
                .LastIndexOf('<') + 1;

            var elementToPos = bufferSpan.Slice(elementStart, pos - elementStart).TrimStart();

            // Find if we are inside a <PackageReference  /> element
            if (!elementToPos.StartsWith(PackageReferenceElement) ||
                elementToPos.Contains(EndElement.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                _router.Window.LogInfo($"Not inside a PackageReference");
                return string.Empty;
            }
            _router.Window.LogInfo($"Inside PackageReference element");

            var attributeEnd = elementToPos.LastIndexOf("=\"");
            var attributeStart = elementToPos
                .Slice(0, attributeEnd + 1)
                .TrimEnd()
                .LastIndexOf(' ') + 1;

            var attribute = elementToPos.Slice(attributeStart, attributeEnd - attributeStart);
            var attributeString = new string(attribute);
            _router.Window.LogInfo($"Inside attribute {attributeString}");

            var attributeValue = elementToPos
                .Slice(attributeEnd + 2);
            var wordToComplete = new string(attributeValue);

            _router.Window.LogInfo($"Word to complete {wordToComplete}");

            return wordToComplete;
        }

        public void SetCapability(CompletionCapability capability)
        {
            _capability = capability;
        }
    }
}
