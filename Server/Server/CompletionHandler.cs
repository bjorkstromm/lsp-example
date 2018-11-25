using Microsoft.Language.Xml;
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
        private readonly NuGetAutoCompleteService _nuGetService;

        private readonly DocumentSelector _documentSelector = new DocumentSelector(
            new DocumentFilter()
            {
                Pattern = "**/*.csproj"
            }
        );

        private CompletionCapability _capability;

        public CompletionHandler(ILanguageServer router, BufferManager bufferManager, NuGetAutoCompleteService nuGetService)
        {
            _router = router;
            _bufferManager = bufferManager;
            _nuGetService = nuGetService;
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

            if (buffer == null)
            {
                return new CompletionList();
            }

            var syntaxTree = Parser.Parse(buffer);

            var position = GetPosition(buffer.GetText(0, buffer.Length),
                (int)request.Position.Line,
                (int)request.Position.Character);

            var node = syntaxTree.FindNode(position);

            var attribute = node.AncestorNodes().OfType<XmlAttributeSyntax>().FirstOrDefault();
            if (attribute != null && node.ParentElement.Name.Equals(PackageReferenceElement))
            {
                if (attribute.Name.Equals(IncludeAttribute))
                {
                    var completions = await _nuGetService.GetPackages(attribute.Value);

                    var diff = position - attribute.ValueNode.Start;

                    return new CompletionList(completions.Select(x => new CompletionItem
                    {
                        Label = x,
                        Kind = CompletionItemKind.Reference,
                        TextEdit = new TextEdit
                        {
                            NewText = x,
                            Range = new Range(
                                new Position
                                {
                                    Line = request.Position.Line,
                                    Character = request.Position.Character - diff + 1
                                }, new Position
                                {
                                    Line = request.Position.Line,
                                    Character = request.Position.Character - diff + attribute.ValueNode.Width - 1
                                })
                        }
                    }), isIncomplete: completions.Count > 1);
                }
                else if (attribute.Name.Equals(VersionAttribute))
                {
                    var includeNode = node.ParentElement.Attributes.FirstOrDefault(x => x.Name.Equals(IncludeAttribute));

                    if (includeNode != null && !string.IsNullOrEmpty(includeNode.Value))
                    {
                        var versions = await _nuGetService.GetPackageVersions(includeNode.Value, attribute.Value);

                        var diff = position - attribute.ValueNode.Start;

                        return new CompletionList(versions.Select(x => new CompletionItem
                        {
                            Label = x,
                            Kind = CompletionItemKind.Reference,
                            TextEdit = new TextEdit
                            {
                                NewText = x,
                                Range = new Range(
                                    new Position
                                    {
                                        Line = request.Position.Line,
                                        Character = request.Position.Character - diff + 1
                                    }, new Position
                                    {
                                        Line = request.Position.Line,
                                        Character = request.Position.Character - diff + attribute.ValueNode.Width - 1
                                    })
                            }
                        }));
                    }
                }
            }

            return new CompletionList();
        }

        private static int GetPosition(string buffer, int line, int col)
        {
            var position = 0;
            for (var i = 0; i < line; i++)
            {
                position = buffer.IndexOf('\n', position) + 1;
            }
            return position + col;
        }

        public void SetCapability(CompletionCapability capability)
        {
            _capability = capability;
        }
    }
}
