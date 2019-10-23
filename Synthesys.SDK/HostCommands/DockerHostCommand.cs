using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using SMACD.AppTree.Evidence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Synthesys.SDK.HostCommands
{
    /// <summary>
    ///     Stream types which can input or output data
    /// </summary>
    public enum StreamTypes : int
    {
        /// <summary>
        ///     Standard input (STDIN)
        /// </summary>
        Stdin = 0,

        /// <summary>
        ///     Standard output (STDOUT)
        /// </summary>
        Stdout = 1,
        
        /// <summary>
        ///     Standard error (STDERR)
        /// </summary>
        Stderr = 2
    }

    /// <summary>
    ///     Represents a mount between the host and a Docker container
    /// </summary>
    public class DockerHostMount
    {
        /// <summary>
        ///     Path on host
        /// </summary>
        public string LocalPath { get; set; }

        /// <summary>
        ///     Path inside container
        /// </summary>
        public string ContainerPath { get; set; }

        /// <summary>
        ///     If the mount is read-only
        /// </summary>
        public bool IsReadOnly { get; set; }
    }

    /// <summary>
    ///     Represents a command run to execute a Docker container
    /// </summary>
    public class DockerHostCommand : HostCommand, IDisposable
    {
        private static Dictionary<string, ManualResetEvent> _imageLocks = new Dictionary<string, ManualResetEvent>();
        private CancellationTokenSource _readLoopCancellation = new CancellationTokenSource();

        /// <summary>
        ///     If the platform is running an accessible Docker daemon
        /// </summary>
        /// <returns></returns>
        public static bool SupportsDocker()
        {
            Uri uri = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                uri = new Uri("npipe://./pipe/docker_engine");
            }
            else
            {
                uri = new Uri("unix:///var/run/docker.sock");
            }
            
            try { var client = new DockerClientConfiguration(uri).CreateClient(); }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Represents a command run to execute a Docker container
        /// </summary>
        /// <param name="image">Image name</param>
        /// <param name="command">Command and arguments to execute</param>
        /// <param name="mounts">Mounts for container</param>
        /// <param name="user">User to execute container as</param>
        public DockerHostCommand(string image, List<string> command, IEnumerable<DockerHostMount> mounts, string user)
        {
            if (mounts == null) mounts = new List<DockerHostMount>();

            ContainerParameters = new CreateContainerParameters()
            {
                Image = image,
                User = user,
                AttachStderr = true,
                AttachStdout = true,
                AttachStdin = false,
                Cmd = command,
                HostConfig = new HostConfig
                {
                    Binds = mounts.Select(m => $"{m.LocalPath}:{m.ContainerPath}:{(m.IsReadOnly ? "ro" : "rw")}")
                        .ToList()
                }
            };
        }

        /// <summary>
        ///     Represents a command run to execute a Docker container
        ///     
        ///     A context will be mounted at /synthesys inside the container
        /// </summary>
        /// <param name="image">Image name</param>
        /// <param name="context">Context to attach to Docker container</param>
        /// <param name="command">Command and arguments to execute</param>
        public DockerHostCommand(string image, NativeDirectoryContext context, params string[] command)
        {
            ContainerParameters = new CreateContainerParameters()
            {
                Image = image,
                AttachStderr = true,
                AttachStdout = true,
                AttachStdin = false,
                Cmd = command,
                HostConfig = new HostConfig
                {
                    Binds = new List<string>()
                    {
                        $"{context.Directory}:/synthesys:rw"
                    }
                }
            };
        }

        /// <summary>
        ///     Represents a command run to execute a Docker container
        /// </summary>
        /// <param name="image">Image name</param>
        /// <param name="command">Command and arguments to execute</param>
        public DockerHostCommand(string image, params string[] command)
        {
            ContainerParameters = new CreateContainerParameters()
            {
                Image = image,
                AttachStderr = true,
                AttachStdout = true,
                AttachStdin = false,
                Cmd = command
            };
        }

        /// <summary>
        ///     Container Working Directory
        /// </summary>
        public string ContainerWorkingDirectory { get => ContainerParameters.WorkingDir; set => ContainerParameters.WorkingDir = value; }

        /// <summary>
        ///     Represents a command run to execute a Docker container
        /// </summary>
        /// <param name="containerParameters">Container parameters for new container</param>
        public DockerHostCommand(CreateContainerParameters containerParameters)
        {
            ContainerParameters = containerParameters;
        }

        /// <summary>
        ///     Parameters to be used during the creation of the Docker container
        /// </summary>
        public CreateContainerParameters ContainerParameters { get; }

        /// <summary>
        ///     Container identifier name
        /// </summary>
        public string ContainerId { get; private set; }

        /// <summary>
        ///     Start container task
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            Uri uri = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                uri = new Uri("npipe://./pipe/docker_engine");
            }
            else
            {
                uri = new Uri("unix:///var/run/docker.sock");
            }

            Logger.LogTrace("Connecting to Docker daemon on {0}", uri);
            using var client = new DockerClientConfiguration(uri).CreateClient();

            ManualResetEvent mre;
            lock (_imageLocks)
            {
                if (_imageLocks.ContainsKey(ContainerParameters.Image))
                    mre = _imageLocks[ContainerParameters.Image];
                else
                    mre = new ManualResetEvent(true);
            }
            if (!mre.WaitOne(0)) Logger.LogInformation("Parking thread while another downloads the Docker image '{0}', which is necessary to run the command...", ContainerParameters.Image);
            mre.WaitOne();

            try
            {
                Logger.LogTrace("Looking up image '{0}' in local library", ContainerParameters.Image);
                var inspected = await client.Images.InspectImageAsync(ContainerParameters.Image);
                Logger.LogTrace("Found image successfully! ID: {0}", inspected.ID);
            }
            catch (DockerImageNotFoundException)
            {
                lock (_imageLocks)
                {
                    _imageLocks[ContainerParameters.Image] = new ManualResetEvent(false);
                }

                Logger.LogWarning("Image '{0}' was not in host image library and will need to be downloaded. This may take a while depending on the size of the image.", ContainerParameters.Image);
                var progressReportAction = new Progress<JSONMessage>(msg =>
                {
                    // Do nothing. This is massively chatty, even for the most verbose logging.
                    // May want to output directly to console and avoid the log...?
                    // msg.Progress doesn't seem to be useful.
                });
                await client.Images.CreateImageAsync(new ImagesCreateParameters() { FromImage = ContainerParameters.Image }, new AuthConfig(), progressReportAction);

                Logger.LogWarning("Image '{0}' download complete", ContainerParameters.Image);

                lock (_imageLocks)
                {
                    _imageLocks[ContainerParameters.Image].Set();
                    _imageLocks.Remove(ContainerParameters.Image);
                }
            }
            catch (Exception e)
            {
                Logger.LogInformation(e, "Image inspect failed!");
            }

            Logger.LogTrace("Creating container around image '{0}'", ContainerParameters.Image);
            CreateContainerResponse container = await client.Containers.CreateContainerAsync(ContainerParameters);
            ContainerId = container.ID;

            Logger.LogTrace("Starting container {0}", ContainerId);
            var attached = await client.Containers.StartContainerAsync(ContainerId, new ContainerStartParameters() { });

            Logger.LogTrace("Attaching streams to container logs for STDOUT/STDERR on {0}", ContainerId);
            var containerLogs = await client.Containers.GetContainerLogsAsync(ContainerId, new ContainerLogsParameters()
            {
                ShowStderr = true,
                ShowStdout = true,
                Follow = true
            });

            _ = Task.Run(() => ReadLoop(containerLogs, (t, s) => 
            { 
                switch (t)
                {
                    case StreamTypes.Stdin:
                        break;
                    case StreamTypes.Stdout:
                        HandleStdOut(s);
                        break;
                    case StreamTypes.Stderr:
                        HandleStdErr(s);
                        break;
                }
            }), _readLoopCancellation.Token);

            IList<ContainerListResponse> containerList = new List<ContainerListResponse>();
            while (true)
            {
                containerList = await client.Containers.ListContainersAsync(new ContainersListParameters() { All = true });
                try
                {
                    if (containerList.Any(c => c.ID == ContainerId && (c.State.ToLower() == "exited" || c.State.ToLower() == "dead")))
                        break;
                    else
                        Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error detecting Container from Docker daemon!");
                    Thread.Sleep(1000);
                }
            }

            if (containerList.Any(c => c.ID == ContainerId))
                await client.Containers.RemoveContainerAsync(ContainerId, new ContainerRemoveParameters() { Force = true });
        }

        private async Task ReadLoop(Stream stream, Action<StreamTypes, string> action)
        {
            byte[] segmentHeader = new byte[8];
            try
            {
                while (true)
                {
                    Array.Clear(segmentHeader, 0, segmentHeader.Length);
                    var cts = new CancellationTokenSource(1000);
                    var count = await stream.ReadAsync(segmentHeader, 0, segmentHeader.Length, cts.Token);
                    StreamTypes streamType = (StreamTypes)segmentHeader[0];

                    if (count > 0 && segmentHeader[1] == 0 && segmentHeader[2] == 0 && segmentHeader[3] == 0 && count < 32767)
                    {
                        var sizeSlice = new ArraySegment<byte>(segmentHeader, 4, 4);
                        if (BitConverter.IsLittleEndian)
                            sizeSlice = sizeSlice.Reverse().ToArray();
                        var size = BitConverter.ToInt32(sizeSlice);

                        if (size > 0)
                        {
                            var buffer = new byte[size];
                            var bytesRead = 0;
                            cts = new CancellationTokenSource(1000);
                            while (bytesRead < size)
                            {
                                bytesRead += await stream.ReadAsync(buffer, bytesRead, buffer.Length - bytesRead, cts.Token);
                            }
                            //if (bytesRead != size)
                            //{
                            //    Logger.LogDebug("Mismatched size in header from available message size. This will misalign the buffer! (Will attempt to fix with an arbitrary 4096-byte realignment buffer)");
                            //}
                            var str = Encoding.UTF8.GetString(buffer, 0, buffer.Length).TrimEnd('\n');
                            action(streamType, str);
                        }
                    }
                    else if (count > 0)
                    {
                        Logger.LogDebug("Malformed message, trying a 4096-byte buffer to realign buffer!");
                        var buffer = new byte[4096];
                        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        var str = Encoding.UTF8.GetString(buffer, 0, buffer.Length).TrimEnd('\n');
                        action(streamType, str);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failure during read from Docker container");
                throw ex;
            }
        }


        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        /// <summary>
        ///     Destructor to dispose
        /// </summary>
        /// <param name="disposing">Currently disposing?</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _readLoopCancellation.Cancel();
                }

                disposedValue = true;
            }
        }

        /// <summary>
        ///     Destructor to dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}