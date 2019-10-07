using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Synthesys.SDK.HostCommands
{
    public class DockerHostMount
    {
        public string LocalPath { get; set; }
        public string ContainerPath { get; set; }
        public bool IsReadOnly { get; set; }
    }

    public class DockerHostCommand : HostCommand, IDisposable
    {
        public DockerHostCommand(string image, IEnumerable<DockerHostMount> mounts, string user) : this(
            new CreateContainerParameters
            {
                Image = image,
                User = user,
                AttachStderr = true,
                AttachStdout = true,
                AttachStdin = false,
                HostConfig = new HostConfig
                {
                    Binds = mounts.Select(m => $"{m.LocalPath}:{m.ContainerPath}:{(m.IsReadOnly ? "ro" : "rw")}")
                        .ToList()
                }
            })
        {
        }

        public DockerHostCommand(CreateContainerParameters containerParameters)
        {
            ContainerParameters = containerParameters;
        }

        public CreateContainerParameters ContainerParameters { get; }
        public string ContainerId { get; private set; }

        public async Task Start()
        {
            DockerClient client;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                client = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();
            }
            else
            {
                client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
            }

            CreateContainerResponse container = await client.Containers.CreateContainerAsync(ContainerParameters);
            ContainerId = container.ID;

            await client.Containers.StartContainerExecAsync(ContainerId);

            MultiplexedStream stdoutStream = await client.Containers.AttachContainerAsync(ContainerId, false,
                new ContainerAttachParameters
                {
                    Stdout = true,
                    Stderr = false,
                    Stdin = false,
                    Stream = true
                });
            MultiplexedStream stderrStream = await client.Containers.AttachContainerAsync(ContainerId, false,
                new ContainerAttachParameters
                {
                    Stdout = false,
                    Stderr = true,
                    Stdin = false,
                    Stream = true
                });
            //var multiplexedStream = await client.Containers.StartAndAttachContainerExecAsync(ContainerId, false);

            Task.Run(() => ReadLoop(stdoutStream, s => HandleStdOut(s)));
            Task.Run(() => ReadLoop(stderrStream, s => HandleStdErr(s)));
        }

        private async Task ReadLoop(MultiplexedStream multiplexedStream, Action<string> action)
        {
            byte[] dockerBuffer = new byte[4096];
            try
            {
                while (true)
                {
                    Array.Clear(dockerBuffer, 0, dockerBuffer.Length);
                    MultiplexedStream.ReadResult dockerReadResult = await multiplexedStream.ReadOutputAsync(dockerBuffer, 0, dockerBuffer.Length,
                        default);

                    if (dockerReadResult.EOF)
                    {
                        break;
                    }

                    if (dockerReadResult.Count > 0)
                    {
                        ArraySegment<byte> segment = new ArraySegment<byte>(dockerBuffer, 0, dockerReadResult.Count);
                        action(Encoding.ASCII.GetString(segment.Array, 0, segment.Count));
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failure during Read from Docker Exec to WebSocket");
            }
        }


        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DockerHostCommand()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion
    }
}