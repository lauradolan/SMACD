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
        /// <summary>
        ///     Represents a command run to execute a Docker container
        /// </summary>
        /// <param name="image">Image name</param>
        /// <param name="mounts">Mounts for container</param>
        /// <param name="user">User to execute container as</param>
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

            using var client = new DockerClientConfiguration(uri).CreateClient();
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


            _ = Task.Run(() => ReadLoop(stdoutStream, s => HandleStdOut(s)));
            _ = Task.Run(() => ReadLoop(stderrStream, s => HandleStdErr(s)));
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