using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SMACD.Data;
using SMACD.ScannerEngine.Factories;
using SMACD.ScannerEngine.Plugins;
using SMACD.ScannerEngine.Resources;

namespace SMACD.ScannerEngine
{
    public class ScanWorkflow
    {
        public ScanWorkflow(string baseWorkingDirectory, string serviceMapFile)
        {
            WorkingDirectory = baseWorkingDirectory;
            ServiceMapFile = serviceMapFile;
        }

        public string WorkingDirectory { get; set; }
        public string ServiceMapFile { get; set; }

        private ILogger Logger { get; } = Global.LogFactory.CreateLogger("ScanWorkflow");

        public async Task Execute()
        {
            Logger.LogDebug("Reading Service Map from {0}", ServiceMapFile);
            var serviceMap = Global.GetServiceMap(ServiceMapFile);

            Logger.LogDebug("Writing mirrored Service Map from {0} to {1}", ServiceMapFile, Path.Combine(WorkingDirectory, "input.yaml"));
            if (!Directory.Exists(WorkingDirectory)) Directory.CreateDirectory(WorkingDirectory);
            File.Copy(ServiceMapFile, Path.Combine(WorkingDirectory, "input.yaml"));

            var attackToolTasks = new List<Task>();
            foreach (var feature in serviceMap.Features)
            foreach (var useCase in feature.UseCases)
            foreach (var abuseCase in useCase.AbuseCases)
            foreach (var pluginPointer in abuseCase.PluginPointers)
                attackToolTasks.Add(ExecuteSingleAttackToolPointer(pluginPointer));
            Task.WhenAll(attackToolTasks).Wait();

            ResourceManager.Instance.Clear();
        }

        private Task ExecuteSingleAttackToolPointer(PluginPointerModel pointer)
        {
            var attackTool = AttackToolPluginFactory.Instance.Emit(pointer.Plugin, pointer.PluginParameters);
            if (pointer.Resource != null)
                attackTool.Resources.Add(ResourceManager.Instance.GetByPointer(pointer.Resource));

            try
            {
                attackTool.Validate();
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Error validating Attack Tool pointer for execution");
                return Task.FromResult(0);
            }

            return TaskManager.Instance.Enqueue(new Task(async () =>
            {
                Logger.LogDebug("Starting scheduled task for plugin pointer {0}", pointer);
                attackTool.Pointer = pointer;
                attackTool.WorkingDirectory = Path.Combine(WorkingDirectory, attackTool.GetChildWorkingDirectory(WorkingDirectory, pointer));
                await attackTool.Execute();
                Logger.LogDebug("Completed scheduled task for plugin pointer {0}", pointer);
            }));
        }
    }
}