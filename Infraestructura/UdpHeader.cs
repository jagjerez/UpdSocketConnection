using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Infraestructura
{
    public class UdpHeader : ProtocolHeader
    {
        private short srcPort;

        private short destPort;

        private short udpLength;

        private short udpChecksum;



        public Ipv6Header ipv6PacketHeader;

        public Ipv4Header ipv4PacketHeader;



        static public int UdpHeaderLength = 8;



        /// <summary>

        /// Simple constructor for the UDP header.

        /// </summary>

        public UdpHeader() : base()

        {

            srcPort = 0;

            destPort = 0;

            udpLength = 0;

            udpChecksum = 0;



            ipv6PacketHeader = null;

            ipv4PacketHeader = null;

        }



        /// <summary>

        /// Gets and sets the destination port. Performs the necessary byte order conversion.

        /// </summary>

        public ushort SourcePort

        {

            get

            {
                ushort valor = (ushort)IPAddress.NetworkToHostOrder(srcPort);
                return valor;

            }

            set

            {
                short valor = (short)value;
                srcPort = IPAddress.HostToNetworkOrder(valor);

            }

        }



        /// <summary>

        /// Gets and sets the destination port. Performs the necessary byte order conversion.

        /// </summary>

        public ushort DestinationPort

        {

            get

            {

                return (ushort)IPAddress.NetworkToHostOrder(destPort);

            }

            set

            {

                destPort = IPAddress.HostToNetworkOrder((short)value);

            }

        }



        /// <summary>

        /// Gets and sets the UDP payload length. This is the length of the payload

        /// plus the size of the UDP header itself.

        /// </summary>

        public ushort Length

        {

            get

            {

                return (ushort)IPAddress.NetworkToHostOrder(udpLength);

            }

            set

            {

                udpLength = IPAddress.HostToNetworkOrder((short)value);

            }

        }



        /// <summary>

        /// Gets and sets the checksum value. It performs the necessary byte order conversion.

        /// </summary>

        public ushort Checksum

        {

            get

            {

                return (ushort)IPAddress.NetworkToHostOrder(udpChecksum);

            }

            set

            {

                udpChecksum = IPAddress.HostToNetworkOrder((short)value);

            }

        }



        /// <summary>

        ///

        /// </summary>

        /// <param name="udpData"></param>

        /// <param name="bytesCopied"></param>

        /// <returns></returns>

        static public UdpHeader Create(byte[] udpData, ref int bytesCopied)

        {

            UdpHeader udpPacketHeader = new UdpHeader();


            udpPacketHeader.srcPort = BitConverter.ToInt16(udpData, 0);

            udpPacketHeader.destPort = BitConverter.ToInt16(udpData, 2);

            udpPacketHeader.udpLength = BitConverter.ToInt16(udpData, 4);

            udpPacketHeader.udpChecksum = BitConverter.ToInt16(udpData, 6);

            bytesCopied = udpPacketHeader.Length;

            return udpPacketHeader;

        }

        static public UdpHeader Create(byte[] udpData, AddressFamily addressFamily)

        {

            UdpHeader udpPacketHeader = new UdpHeader();

            if(addressFamily == AddressFamily.InterNetwork)
            {
                udpPacketHeader.srcPort = BitConverter.ToInt16(udpData, Ipv4Header.Ipv4HeaderLength);

                udpPacketHeader.destPort = BitConverter.ToInt16(udpData, Ipv4Header.Ipv4HeaderLength + 2);

                udpPacketHeader.udpLength = BitConverter.ToInt16(udpData, Ipv4Header.Ipv4HeaderLength + 4);

                udpPacketHeader.udpChecksum = BitConverter.ToInt16(udpData, Ipv4Header.Ipv4HeaderLength + 6);
            }
            else
            {
                udpPacketHeader.srcPort = BitConverter.ToInt16(udpData, 0);

                udpPacketHeader.destPort = BitConverter.ToInt16(udpData, 2);

                udpPacketHeader.udpLength = BitConverter.ToInt16(udpData, 4);

                udpPacketHeader.udpChecksum = BitConverter.ToInt16(udpData, 6);
            }
            

            return udpPacketHeader;

        }









        /// <summary>

        /// This method builds the byte array representation of the UDP header as it would appear

        /// on the wire. To do this it must build the IPv4 or IPv6 pseudo header in order to

        /// calculate the checksum on the packet. This requires knowledge of the IPv4 or IPv6 header

        /// so one of these must be set before a UDP packet can be set.

        ///

        /// The IPv4 pseudo header consists of:

        ///   4-byte source IP address

        ///   4-byte destination address

        ///   1-byte zero field

        ///   1-byte protocol field

        ///   2-byte UDP length

        ///   2-byte source port

        ///   2-byte destination port

        ///   2-byte UDP packet length

        ///   2-byte UDP checksum (zero)

        ///   UDP payload (padded to the next 16-bit boundary)

        /// The IPv6 pseudo header consists of:

        ///   16-byte source address

        ///   16-byte destination address

        ///   4-byte payload length

        ///   3-byte zero pad

        ///   1-byte protocol value

        ///   2-byte source port

        ///   2-byte destination port

        ///   2-byte UDP length

        ///   2-byte UDP checksum (zero)

        ///   UDP payload (padded to the next 16-bit boundary)

        /// </summary>

        /// <param name="payLoad">Payload that follows the UDP header</param>

        /// <returns></returns>

        public override byte[] GetProtocolPacketBytes(byte[] payLoad)

        {

            byte[] udpPacket = new byte[UdpHeaderLength + payLoad.Length], pseudoHeader = null, byteValue = null;

            int offset = 0;



            // Build the UDP packet first

            byteValue = BitConverter.GetBytes(srcPort);

            Array.Copy(byteValue, 0, udpPacket, offset, byteValue.Length);

            offset += byteValue.Length;



            byteValue = BitConverter.GetBytes(destPort);

            Array.Copy(byteValue, 0, udpPacket, offset, byteValue.Length);

            offset += byteValue.Length;



            byteValue = BitConverter.GetBytes(udpLength);

            Array.Copy(byteValue, 0, udpPacket, offset, byteValue.Length);

            offset += byteValue.Length;



            udpPacket[offset++] = 0;      // Checksum is initially zero

            udpPacket[offset++] = 0;



            // Copy payload to end of packet

            Array.Copy(payLoad, 0, udpPacket, offset, payLoad.Length);



            if (ipv4PacketHeader != null)

            {

                pseudoHeader = new byte[UdpHeaderLength + 12 + payLoad.Length];



                // Build the IPv4 pseudo header

                offset = 0;



                // Source address

                byteValue = ipv4PacketHeader.SourceAddress.GetAddressBytes();

                Array.Copy(byteValue, 0, pseudoHeader, offset, byteValue.Length);

                offset += byteValue.Length;



                // Destination address

                byteValue = ipv4PacketHeader.DestinationAddress.GetAddressBytes();

                Array.Copy(byteValue, 0, pseudoHeader, offset, byteValue.Length);

                offset += byteValue.Length;



                // 1 byte zero pad plus next header protocol value

                pseudoHeader[offset++] = 0;

                pseudoHeader[offset++] = ipv4PacketHeader.Protocol;



                // Packet length

                byteValue = BitConverter.GetBytes(udpLength);

                Array.Copy(byteValue, 0, pseudoHeader, offset, byteValue.Length);

                offset += byteValue.Length;



                // Copy the UDP packet to the end of this

                Array.Copy(udpPacket, 0, pseudoHeader, offset, udpPacket.Length);

            }

            else if (ipv6PacketHeader != null)

            {

                uint ipv6PayloadLength;



                pseudoHeader = new byte[UdpHeaderLength + 40 + payLoad.Length];



                // Build the IPv6 pseudo header

                offset = 0;



                // Source address

                byteValue = ipv6PacketHeader.SourceAddress.GetAddressBytes();

                Array.Copy(byteValue, 0, pseudoHeader, offset, byteValue.Length);

                offset += byteValue.Length;



                // Destination address

                byteValue = ipv6PacketHeader.DestinationAddress.GetAddressBytes();

                Array.Copy(byteValue, 0, pseudoHeader, offset, byteValue.Length);

                offset += byteValue.Length;



                ipv6PayloadLength = (uint)IPAddress.HostToNetworkOrder((int)(payLoad.Length + UdpHeaderLength));



                // Packet payload: ICMPv6 headers plus payload

                byteValue = BitConverter.GetBytes(ipv6PayloadLength);

                Array.Copy(byteValue, 0, pseudoHeader, offset, byteValue.Length);

                offset += byteValue.Length;



                // 3 bytes zero pad plus next header protocol value

                pseudoHeader[offset++] = 0;

                pseudoHeader[offset++] = 0;

                pseudoHeader[offset++] = 0;

                pseudoHeader[offset++] = ipv6PacketHeader.NextHeader;



                // Copy the UDP packet to the end of this

                Array.Copy(udpPacket, 0, pseudoHeader, offset, udpPacket.Length);

            }



            if (pseudoHeader != null)

            {

                Checksum = ComputeChecksum(pseudoHeader);

            }



            // Put checksum back into packet

            byteValue = BitConverter.GetBytes(udpChecksum);

            Array.Copy(byteValue, 0, udpPacket, 6, byteValue.Length);



            return udpPacket;

        }
    }
}
