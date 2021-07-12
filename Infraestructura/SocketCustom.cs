using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Infraestructura.UDPSocketFactory;

namespace Infraestructura
{
    /// <summary>
    /// Udp class Socket Factory
    /// </summary>
    public class UDPSocketFactory : SocketCustom
    {
        //singleton Instance
        private static UDPSocketFactory instance;
        private UDPSocketFactory(IServiceProvider pProveedorServicios) : base(pProveedorServicios) { }
        public static UDPSocketFactory CrearInstance(IServiceProvider pProveedorServicios)
        {
            if (instance == null)
            {
                instance = new UDPSocketFactory(pProveedorServicios);
            }
            return instance;
        }
        public static void DestroyInstance()
        {
            if (instance != null)
            {
                GC.SuppressFinalize(instance);
                instance = null;
            }
        }
        public SocketCustom ConfigureSocket(
            SocketOptionName socketOptionName = SocketOptionName.HeaderIncluded,
            ProtocolType protocolType = ProtocolType.Udp,
            SocketType socketType = SocketType.Raw,
            SocketOptionLevel socketLevel = SocketOptionLevel.IP,
            CancellationToken cancellationToken = default(CancellationToken)
            )
        {
            //remote data
            //base.remoteReceiveAdress = IPAddress.Parse(configuration.GetValue<string>("SOCKET_REMOTE_HOST"));
            //base.remoteReceivePort = configuration.GetValue<ushort>("SOCKET_REMOTE_PORT");

            //source data
            base.sourceAddress = IPAddress.Parse(base.configuration.GetValue<string>("SOCKET_SOURCE_HOST"));
            base.sourcePort = base.configuration.GetValue<ushort>("SOCKET_SOURCE_PORT");

            //destine data
            base.destAddress = IPAddress.Parse(base.configuration.GetValue<string>("SOCKET_DESTINATION_HOST"));
            base.destPort = base.configuration.GetValue<ushort>("SOCKET_DESTINATION_PORT");
            
            //binding data
            base.bindAddress = IPAddress.Any;
            
            //configuration data
            base.messageSize = base.configuration.GetValue<int>("SOCKET_SEND_MESSAGE_SIZE");
            base.messageReceiveSize = base.configuration.GetValue<int>("SOCKET_RECEIVE_MESSAGE_SIZE");
            base.sendCount = base.configuration.GetValue<int>("SOCKET_NUMBER_SEND");
            base.cancellationToken = cancellationToken;
            base.socketLevel = socketLevel;
            base.socketType = socketType;
            base.protocolType = protocolType;
            base.socketOptionName = socketOptionName;
            
            base.ConnectServer();
            return this;
        }
    }
    public abstract class SocketCustom
    {
        internal IPAddress sourceAddress, destAddress, bindAddress;
        internal readonly IConfiguration configuration;
        internal ushort sourcePort, destPort;
        internal int messageSize, sendCount;
        internal int messageReceiveSize;
        internal Socket socketInstance;
        internal ILogger<SocketCustom> logger;
        internal CancellationToken cancellationToken;
        internal SocketOptionName socketOptionName = SocketOptionName.HeaderIncluded;
        internal ProtocolType protocolType = ProtocolType.Udp;
        internal SocketType socketType = SocketType.Raw;
        internal SocketOptionLevel socketLevel = SocketOptionLevel.IP;
        internal Task thread;

        public bool IsConnected => this.socketInstance != null && this.socketInstance.Connected;
        public delegate void HandlerReceivedData(byte[] data,AddressFamily addressFamily);
        public event HandlerReceivedData DataReceived;
        
        protected virtual void OnDataReceived(byte[] data, AddressFamily addressFamily)
        {
            HandlerReceivedData handler = DataReceived;
            if(handler != null)
            {
                handler?.Invoke(data, addressFamily);
            }
        }

        public void WaitReceived(int ms)
        {
            thread.Wait(ms);
        }

        public SocketCustom(IServiceProvider pProveedorServicios)
        {
            configuration = pProveedorServicios.GetService<IConfiguration>();
            logger = pProveedorServicios.GetService<ILogger<SocketCustom>>();
            

        }
        public SocketCustom Send(byte[] data)
        {

            lock (this.socketInstance)
            {
                if (this.socketInstance == null)
                {
                    throw new ApplicationException("Debe ejecutar antes el metodo CreateServer");
                }

                try
                {
                    byte[] dataIntoPackage = GeneratePackage(data);
                     
                    // Send the packet!
                    logger.Log(LogLevel.Information, "Sending the packet...");
                    for (int i = 0; i < sendCount; i++)
                    {

                        int rc = this.socketInstance.SendTo(dataIntoPackage, new IPEndPoint(destAddress, destPort));
                        logger.Log(LogLevel.Information, "send {0} bytes to {1}", rc, destAddress.ToString());
                    }
                    string message = SocketCustom.Create(dataIntoPackage, "Data Send:", destAddress.AddressFamily);
                    logger.Log(LogLevel.Information, "{0}", message);

                }
                catch (SocketException err)
                {
                    logger.Log(LogLevel.Error, "Socket error occurred: {0}", err.Message);
                    // http://msdn.microsoft.com/en-us/library/ms740668.aspx
                }
            }

            return this;
        }
        public void Close()
        {
            lock (this.socketInstance)
            {
                logger.Log(LogLevel.Information, "Closing the socket...");
                this.socketInstance.Close();
            }
                
        }

        protected void ConnectServer()
        {
            if ((sourceAddress.AddressFamily != destAddress.AddressFamily) ||
                (sourceAddress.AddressFamily != bindAddress.AddressFamily))
            {
                throw new ApplicationException("Source and destination address families don't match!");
            }

            // Create the raw socket for this packet
            logger.Log(LogLevel.Information, "Creating the raw socket using Socket()...");
            this.socketInstance = new Socket(sourceAddress.AddressFamily, socketType, protocolType);

            // Bind the socket to the interface specified
            logger.Log(LogLevel.Information, "Binding the socket to the specified interface using Bind()...");
            this.socketInstance.Bind(new IPEndPoint(bindAddress, 0));

            // Set the HeaderIncluded option since we include the IP header
            logger.Log(LogLevel.Information, "Setting the HeaderIncluded option for IP header...");

            this.socketInstance.SetSocketOption(socketLevel, socketOptionName, 1);


            ReceiveData();
        }

        private void ReceiveData()
        {
            
            int bufferSize = 0;
            // Start building the headers
            logger.Log(LogLevel.None, "Building the packet header...");
            byte[] payLoad = new byte[messageReceiveSize];
            bufferSize = messageReceiveSize;
            if (sourceAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                bufferSize = Ipv4Header.Ipv4HeaderLength + UdpHeader.UdpHeaderLength + messageReceiveSize;
            }
            else if (sourceAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                bufferSize = UdpHeader.UdpHeaderLength + messageReceiveSize;
            }
            
            thread = Task.Run(async () =>
              {
                  SocketReceiveMessageFromResult res;
                  while (!cancellationToken.IsCancellationRequested)
                  {
                      
                      try
                      {
                          State state = new State(bufferSize);
                          EndPoint epFromMensaje = new IPEndPoint(bindAddress, 0);
                          ArraySegment<byte> _buffer_recv_segment = new ArraySegment<byte>(state.buffer);
                          bool enviar = true;
                          res = await this.socketInstance.ReceiveMessageFromAsync(_buffer_recv_segment, SocketFlags.None, epFromMensaje);
                          UdpHeader udpHeader = UdpHeader.Create(state.buffer, res.RemoteEndPoint.AddressFamily);
                          if(udpHeader.DestinationPort != sourcePort)
                          {
                              enviar = false;
                          }
                          Ipv4Header ipv4Header = null;
                          if (enviar)
                          {
                              if (res.RemoteEndPoint.AddressFamily == AddressFamily.InterNetwork)
                              {
                                  int bytes = 0;
                                  ipv4Header = Ipv4Header.Create(state.buffer, ref bytes);
                                  if (!ipv4Header.DestinationAddress.ToString().Equals(sourceAddress.ToString()))
                                  {
                                      enviar = false;
                                  }
                              }
                          }

                          //Array.Copy(_buffer_recv_segment.Array, 0, state.buffer, 0, res.ReceivedBytes);
                          if (enviar)
                          {
                              OnDataReceived(state.buffer, epFromMensaje.AddressFamily);
                          }  
                      }
                      catch (Exception err)
                      {
                          logger.Log(LogLevel.Error, "Received data error: {0}", err.Message);
                      }
                  }
              });
             
            
        }

        //private void Receive(IAsyncResult ar)
        //{
            
        //    //lock (this.socketInstance)
        //    //{
        //    //    State so = (State)ar.AsyncState;
        //    //    EndPoint epFromMensaje = new IPEndPoint(remoteAdress, remotePort);
        //    //    try
        //    //    {
                    
        //    //        if (this.socketInstance != null)
        //    //        {
                        
                        
                        
        //    //            //int numberBytesReceive = this.socketInstance.EndReceiveFrom(ar, ref epFromMensaje);
        //    //            this.socketInstance.BeginReceiveFrom(so.buffer, 0, so.buffer.Length, SocketFlags.None, ref epFromMensaje, Receive, so);
        //    //            DispararDatosRecibidos(so.buffer, epFromMensaje.AddressFamily);
        //    //        }
                    
        //    //    }
        //    //    catch (SocketException ex)
        //    //    {
        //    //        logger.LogError(ex.Message);
        //    //        this.socketInstance.BeginReceiveFrom(so.buffer, 0, so.buffer.Length, SocketFlags.None, ref epFromMensaje, Receive, so);
        //    //    }
        //    //}
        //}

        private byte[] GeneratePackage(byte[] data)
        {
            if (data.Length < messageSize)
            {
                messageSize = data.Length;
            }

            // Start building the headers
            logger.Log(LogLevel.None, "Building the packet header...");
            byte[] builtPacket, payLoad = new byte[messageSize];
            UdpHeader udpPacket = new UdpHeader();
            ArrayList headerList = new ArrayList();


            // Initialize the payload
            logger.Log(LogLevel.None, "Initialize the payload...");
            for (int i = 0; i < payLoad.Length; i++)
                payLoad[i] = data[i];

            // Fill out the UDP header first
            logger.Log(LogLevel.None, "Filling out the UDP header...");
            udpPacket.SourcePort = sourcePort;
            udpPacket.DestinationPort = destPort;
            udpPacket.Length = (ushort)(UdpHeader.UdpHeaderLength + messageSize);
            udpPacket.Checksum = 0;
            if (sourceAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                Ipv4Header ipv4Packet = new Ipv4Header();
                // Build the IPv4 header
                logger.Log(LogLevel.None, "Building the IPv4 header...");
                ipv4Packet.Version = 4;
                ipv4Packet.Protocol = (byte)ProtocolType.Udp;
                ipv4Packet.Ttl = 2;
                ipv4Packet.Offset = 0;
                ipv4Packet.Length = (byte)Ipv4Header.Ipv4HeaderLength;
                ipv4Packet.TotalLength = (ushort)System.Convert.ToUInt16(Ipv4Header.Ipv4HeaderLength + UdpHeader.UdpHeaderLength + messageSize);
                ipv4Packet.SourceAddress = sourceAddress;
                ipv4Packet.DestinationAddress = destAddress;

                // Set the IPv4 header in the UDP header since it is required to calculate the
                //    pseudo header checksum
                logger.Log(LogLevel.None, "Setting the IPv4 header for pseudo header checksum...");
                udpPacket.ipv4PacketHeader = ipv4Packet;

                // Add IPv4 header to list of headers -- headers should be added in th order
                //    they appear in the packet (i.e. IP first then UDP)
                logger.Log(LogLevel.None, "Adding the IPv4 header to the list of header, encapsulating packet...");
                headerList.Add(ipv4Packet);
                //socketLevel = SocketOptionLevel.IP;
            }
            else if (sourceAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                Ipv6Header ipv6Packet = new Ipv6Header();
                // Build the IPv6 header
                logger.Log(LogLevel.None, "Building the IPv6 header...");
                ipv6Packet.Version = 6;
                ipv6Packet.TrafficClass = 1;
                ipv6Packet.Flow = 2;
                ipv6Packet.HopLimit = 2;
                ipv6Packet.NextHeader = (byte)ProtocolType.Udp;
                ipv6Packet.PayloadLength = (ushort)(UdpHeader.UdpHeaderLength + payLoad.Length);
                ipv6Packet.SourceAddress = sourceAddress;
                ipv6Packet.DestinationAddress = destAddress;

                // Set the IPv6 header in the UDP header since it is required to calculate the
                //    pseudo header checksum
                logger.Log(LogLevel.None, "Setting the IPv6 header for pseudo header checksum...");
                udpPacket.ipv6PacketHeader = ipv6Packet;

                // Add the IPv6 header to the list of headers - headers should be added in the order
                //    they appear in the packet (i.e. IP first then UDP)
                logger.Log(LogLevel.None, "Adding the IPv6 header to the list of header, encapsulating packet...");
                headerList.Add(ipv6Packet);
                //socketLevel = SocketOptionLevel.IPv6;
            }



            // Add the UDP header to list of headers after the IP header has been added
            logger.Log(LogLevel.None, "Adding the UDP header to the list of header, after IP header...");
            headerList.Add(udpPacket);

            // Convert the header classes into the binary on-the-wire representation
            logger.Log(LogLevel.None, "Converting the header classes into the binary...");
            builtPacket = udpPacket.BuildPacket(headerList, payLoad);
            return builtPacket;
        }

        public static string Create(byte[] allData,string title,AddressFamily addressFamily)
        {
            StringBuilder stb = new StringBuilder();
            int bytes = 0;
            int dataLength = 0;
            byte[] ipV4 = new byte[Ipv4Header.Ipv4HeaderLength];
            byte[] headerData = new byte[UdpHeader.UdpHeaderLength];
            byte[] data = null;
            Ipv4Header ipv4Header = null;

            stb.AppendLine($"|{title}:");

            if (addressFamily == AddressFamily.InterNetwork)
            {
                dataLength = allData.Length - (Ipv4Header.Ipv4HeaderLength + UdpHeader.UdpHeaderLength);
                data = new byte[dataLength];
                Array.Copy(allData, 0, ipV4, 0, Ipv4Header.Ipv4HeaderLength);
                Array.Copy(allData, Ipv4Header.Ipv4HeaderLength, headerData, 0, UdpHeader.UdpHeaderLength);
                Array.Copy(allData, (Ipv4Header.Ipv4HeaderLength + UdpHeader.UdpHeaderLength), data, 0, data.Length);
                ipv4Header = Ipv4Header.Create(ipV4, ref bytes);
                stb.AppendLine($"|__________________________________________________________________________________________________");
                stb.AppendLine($"|IpVx header -> Souce Ip:{ipv4Header.SourceAddress.ToString()}, Destine ip: {ipv4Header.DestinationAddress.ToString()}");
                stb.AppendLine($"|--------------------------------------------------------------------------------------------------");
                stb.AppendLine($"|IpVx header in bytes -> {string.Join(",", ipV4)}");
                stb.AppendLine($"|--------------------------------------------------------------------------------------------------");
                stb.AppendLine($"|Estructure reference:");
                stb.AppendLine($"     |Byte 1|     |Byte 2|   |Bytes 3 & 4| |Bytes 5 & 6| |Bytes 7 & 8|  |Byte 9| |Byte 10| |Bytes 11 & 12| |Bytes 13,14,15 & 16| |Bytes 17,18,19 & 20|");
                stb.AppendLine($"|Version procol|Type service|Total length |      Id     |   Offset    |Ttl value|Protocolo|   Checksum    |    Source address   | Destination address |");
                stb.AppendLine($"|--------------------------------------------------------------------------------------------------");
                stb.AppendLine($"|IpVx header length:{Ipv4Header.Ipv4HeaderLength} bytes");
                
            }
            else if(addressFamily == AddressFamily.InterNetworkV6)
            {
                throw new ApplicationException("No implementation");
            }
            else
            {
                dataLength = allData.Length - UdpHeader.UdpHeaderLength;
                data = new byte[dataLength];
                Array.Copy(allData, 0, headerData, 0, UdpHeader.UdpHeaderLength);
                Array.Copy(allData, UdpHeader.UdpHeaderLength, data, 0, data.Length);
            }
            UdpHeader udpHeader = UdpHeader.Create(headerData, ref bytes);
            stb.AppendLine($"|__________________________________________________________________________________________________");
            stb.AppendLine($"|Upd deader -> source port:{udpHeader.SourcePort}, destine port:{udpHeader.DestinationPort}");
            stb.AppendLine($"|--------------------------------------------------------------------------------------------------");
            stb.AppendLine($"|Upd deader in byte -> {string.Join(",", headerData)}");
            stb.AppendLine($"|--------------------------------------------------------------------------------------------------");
            stb.AppendLine($"|Estructure reference:");
            stb.AppendLine($"  |Bytes 1 & 2|   |Bytes 3 & 4|    |Bytes 5 & 6|   |Bytes 7 y 8|");
            stb.AppendLine($"| Source port  |Destination port|Udp header length|   Checksum  |");
            stb.AppendLine($"|--------------------------------------------------------------------------------------------------");
            stb.AppendLine($"|Udp header length:{UdpHeader.UdpHeaderLength} bytes");
            stb.AppendLine($"|__________________________________________________________________________________________________");
            stb.AppendLine($"|Data -> {Encoding.ASCII.GetString(data).Trim()}");
            stb.AppendLine($"|--------------------------------------------------------------------------------------------------");
            stb.AppendLine($"|Udp data in byte -> {string.Join(",", data)}");
            stb.AppendLine($"|--------------------------------------------------------------------------------------------------");
            stb.AppendLine($"|Data length:{dataLength} bytes");
            return stb.ToString();
        }

    }
    public class State
    {
        public State(int numeroBytes)
        {
            buffer = new byte[numeroBytes];
        }
        public byte[] buffer;
    }
}
