﻿//-----------------------------------------------------------------------
// <copyright file="ListCommandHandler.cs" company="Fubar Development Junker">
//     Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>
// <author>Mark Junker</author>
//-----------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FubarDev.FtpServer.ListFormatters;

using Minimatch;

using Sockets.Plugin.Abstractions;

namespace FubarDev.FtpServer.CommandHandlers
{
    /// <summary>
    /// Implements the <code>LIST</code> and <code>NLST</code> commands.
    /// </summary>
    public class ListCommandHandler : FtpCommandHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListCommandHandler"/> class.
        /// </summary>
        /// <param name="connection">The connection to create this command handler for</param>
        public ListCommandHandler(FtpConnection connection)
            : base(connection, "LIST", "NLST")
        {
        }

        /// <inheritdoc/>
        public override async Task<FtpResponse> Process(FtpCommand command, CancellationToken cancellationToken)
        {
            await Connection.WriteAsync(new FtpResponse(150, "Opening data connection."), cancellationToken);
            ITcpSocketClient responseSocket;
            try
            {
                responseSocket = await Connection.CreateResponseSocket();
            }
            catch (Exception)
            {
                return new FtpResponse(425, "Can't open data connection.");
            }
            try
            {
                var argument = command.Argument;
                IListFormatter formatter;
                if (string.Equals(command.Name, "NLST", StringComparison.OrdinalIgnoreCase))
                {
                    formatter = new ShortListFormatter();
                }
                else
                {
                    formatter = new LongListFormatter();
                }
                var mask = (string.IsNullOrEmpty(argument) || argument.StartsWith("-")) ? "*" : argument;

                var encoding = Data.NlstEncoding ?? Connection.Encoding;

                using (var stream = await Connection.CreateEncryptedStream(responseSocket.WriteStream))
                {
                    using (var writer = new StreamWriter(stream, encoding, 4096, true)
                    {
                        NewLine = "\r\n",
                    })
                    {
                        foreach (var line in formatter.GetPrefix(Data.CurrentDirectory))
                        {
                            Connection.Log?.Debug(line);
                            await writer.WriteLineAsync(line);
                        }

                        var mmOptions = new Options()
                        {
                            IgnoreCase = Data.FileSystem.FileSystemEntryComparer.Equals("a", "A"),
                            NoGlobStar = true,
                            Dot = true,
                        };

                        var mm = new Minimatcher(mask, mmOptions);

                        foreach (var entry in (await Data.FileSystem.GetEntriesAsync(Data.CurrentDirectory, cancellationToken)).Where(x => mm.IsMatch(x.Name)))
                        {
                            var line = formatter.Format(entry);
                            Connection.Log?.Debug(line);
                            await writer.WriteLineAsync(line);
                        }

                        foreach (var line in formatter.GetSuffix(Data.CurrentDirectory))
                        {
                            Connection.Log?.Debug(line);
                            await writer.WriteLineAsync(line);
                        }
                    }
                }
            }
            finally
            {
                responseSocket.Dispose();
            }

            // Use 250 when the connection stays open.
            return new FtpResponse(250, "Closing data connection.");
        }
    }
}
