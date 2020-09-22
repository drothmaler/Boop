﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using ChakraCore.NET;
using Woop.Models;
using System.Linq;

namespace Woop.Services
{
    class ScriptManager
    {
        private readonly ChakraRuntime _runtime;
        private readonly SettingsService _settingsService;

        public ScriptManager(SettingsService settingsService)
        {
            _settingsService = settingsService;

            _runtime = ChakraRuntime.Create();
            _runtime.ServiceNode.GetService<IJSValueConverterService>().RegisterStructConverter((value, instance) =>
            {
                value.WriteProperty("fullText", instance.FullText);
                value.WriteProperty("selection", instance.Selection ?? string.Empty);
                value.WriteProperty("isSelection", instance.IsSelection);
            }, (value) =>
            {
                var fullText = value.ReadProperty<string>("fullText");
                var selection = value.ReadProperty<string>("selection");
                return new ScriptExecutionProperties(selection, fullText);
            });

            _runtime.ServiceNode.GetService<IJSValueConverterService>().RegisterProxyConverter<ScriptExecutionMethods>((binding, instance, serviceNode) =>
            {
                binding.SetMethod<string>("postInfo", instance.PostInfo);
                binding.SetMethod<string>("postError", instance.PostError);
                binding.SetMethod<string>("insert", instance.Insert);
            });
        }

        public async Task<IEnumerable<Script>> InitializeAsync()
        {
            var appInstalledFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var scriptsFolder = await appInstalledFolder.GetFolderAsync("Assets\\Scripts");

            var adapterFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/WoopAdapter.js"));
            var adapterScript = await ReadScriptContentAsync(adapterFile);

            var builtInScripts = await InitializeScripts(scriptsFolder, adapterScript, true);

            var customScripts = Enumerable.Empty<Script>();
            if (!string.IsNullOrWhiteSpace(_settingsService.CustomScriptsFolderLocation))
            {
                var folder = await StorageFolder.GetFolderFromPathAsync(_settingsService.CustomScriptsFolderLocation);
                customScripts = await InitializeScripts(folder, adapterScript, false);
            }

            return builtInScripts.Concat(customScripts);
        }

        private async Task<IEnumerable<Script>> InitializeScripts(StorageFolder folder, string adapterScript, bool builtIn)
        {
            var scripts = new List<Script>();
            foreach (var file in await folder.GetFilesAsync())
            {
                try
                {
                    var script = await InitializeScript(file, adapterScript, builtIn);
                    scripts.Add(script);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"{file.Name} failed: {e.Message}");
                    continue;
                }
            }

            return scripts;
        }

        private async Task<Script> InitializeScript(StorageFile file, string adapterScript, bool builtIn)
        {
            var content = await ReadScriptContentAsync(file);
            var context = _runtime.CreateContext(true);
#pragma warning disable CS0618 // Type or member is obsolete
            JSRequireLoader.EnableRequire(context);
#pragma warning restore CS0618 // Type or member is obsolete

            var result = context.RunScript(content);
            result = context.RunScript(adapterScript);
            return new Script(context, content, builtIn);
        }

        private async Task<string> ReadScriptContentAsync(StorageFile file)
        {
            var script = await FileIO.ReadTextAsync(file);
            script = script.Replace("require('@boop/", $"require('Assets/Scripts/lib/");
            return script;
        }
    }
}
