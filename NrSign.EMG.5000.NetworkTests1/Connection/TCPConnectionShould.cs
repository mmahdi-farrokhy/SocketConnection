using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketConnection.Data;
using System;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace SocketConnection.Hardware.Tests
{
    [TestClass()]
    public class TCPConnectionShould
    {
        private TCPConnection _connection;
        byte[] stimCommandPacket = new byte[] { 0XFF, 0XFF, 0XFA, 0X05, 0X11, 0X22, 0X33, 0X44, 0X55, 0X66, 0X77, 0X88, 0X99, 0XAA, 0XBB, 0XCC, 0XDD, 0XEE, 0XFF };
        SerialData expectedStimCommand = new SerialData(0XFA, 5)
        {
            Header = new byte[] { 0XFF, 0XFF, 0XFA },
            Body = new byte[] { 0X11, 0X22, 0X33, 0X44, 0X55 }
        };

        byte[] headBoxCommandPacket = new byte[] { 0XFF, 0XFF, 0XFB, 0X05, 0X11, 0X22, 0X33, 0X44, 0X55, 0X66, 0X77, 0X88, 0X99, 0XAA, 0XBB, 0XCC, 0XDD, 0XEE, 0XFF };
        SerialData expectedHeadBoxCommand = new SerialData(0XFB, 5)
        {
            Header = new byte[] { 0XFF, 0XFF, 0XFB },
            Body = new byte[] { 0X11, 0X22, 0X33, 0X44, 0X55 }
        };

        byte[] dataPacket = new byte[] { 0XFF, 0XFF, 0X01, 0X02, 0X03, 0X04, 0X05, 0X06, 0X07, 0X08, 0X09, 0X10, 0X11, 0X12, 0X13, 0X14, 0X15, 0X16, 0X17,
                                             0XFF, 0XFF, 0X18, 0X19, 0X20, 0X21, 0X22, 0X23, 0X24, 0X25, 0X26, 0X27, 0X28, 0X29, 0X30, 0X31, 0X32, 0X33, 0X34,
                                             0XFF, 0XFF, 0X35, 0X36, 0X37, 0X38, 0X39, 0X3A, 0X3B, 0X3C, 0X3D, 0X3E, 0X3F, 0X40, 0X41, 0X42, 0X43, 0X44, 0X45,
                                             0XFF, 0XFF, 0X46, 0X47, 0X48, 0X49, 0X4A, 0X4B, 0X4C, 0X4D, 0X4E, 0X4F, 0X50, 0X51, 0X52, 0X53, 0X54, 0X55, 0X56,
                                             0XFF, 0XFF, 0X57, 0X58, 0X59, 0X5A, 0X5B, 0X5C, 0X5D, 0X5E, 0X5F, 0X60, 0X61, 0X62, 0X63, 0X64, 0X65, 0X66, 0X67,
                                             0XFF, 0XFF, 0X68, 0X69, 0X6A, 0X6B, 0X6C, 0X6D, 0X6E, 0X6F, 0X70, 0X71, 0X72, 0X73, 0X74, 0X75, 0X76, 0X77, 0X78,
                                             0XFF, 0XFF, 0X79, 0X7A, 0X7B, 0X7C, 0X7D, 0X7E, 0X7F, 0X80, 0X81, 0X82, 0X83, 0X84, 0X85, 0X86, 0X87, 0X88, 0X89,
                                             0XFF, 0XFF, 0X8A, 0X8B, 0X8C, 0X8D, 0X8E, 0X8F, 0X90, 0X91, 0X92, 0X93, 0X94, 0X95, 0X96, 0X97, 0X98, 0X99, 0X9A,
                                             0XFF, 0XFF, 0X9B, 0X9C, 0X9D, 0X9E, 0X9F, 0XA0, 0XA1, 0XA2, 0XA3, 0XA4, 0XA5, 0XA6, 0XA7, 0XA8, 0XA9, 0XAA, 0XAB,
                                             0XFF, 0XFF, 0XAC, 0XAD, 0XAE, 0XAF, 0XB0, 0XB1, 0XB2, 0XB3, 0XB4, 0XB5, 0XB6, 0XB7, 0XB8, 0XB9, 0XBA, 0XBB, 0XBC,
                                             0XFF, 0XFF, 0XBD, 0XBE, 0XBF, 0XC0, 0XC1, 0XC2, 0XC3, 0XC4, 0XC5, 0XC6, 0XC7, 0XC8, 0XC9, 0XCA, 0XCB, 0XCC, 0XCD,
                                             0XFF, 0XFF, 0XCE, 0XCF, 0XD0, 0XD1, 0XD2, 0XD3, 0XD4, 0XD5, 0XD6, 0XD7, 0XD8, 0XD9, 0XDA, 0XDB, 0XDC, 0XDD, 0XDE,
                                             0XFF, 0XFF, 0XDF, 0XE0, 0XE1, 0XE2, 0XE3, 0XE4, 0XE5, 0XE6, 0XE7, 0XE8, 0XE9, 0XEA, 0XEB, 0XEC, 0XED, 0XEE, 0XEF,
                                             0XFF, 0XFF, 0XF0, 0XF1, 0XF2, 0XF3, 0XF4, 0XF5, 0XF6, 0XF7, 0XF8, 0XF9, 0XFA, 0XFB, 0XFC, 0XFD, 0XFE, 0XFF, 0X00,
                                             0XFF, 0XFF, 0X01, 0X02, 0X03, 0X04, 0X05, 0X06, 0X07, 0X08, 0X09, 0X0A, 0X0B, 0X0C, 0X0D, 0X0E, 0X0F, 0X10, 0X11,
                                             0XFF, 0XFF, 0X12, 0X13, 0X14, 0X15, 0X16, 0X17, 0X18, 0X19, 0X1A, 0X1B, 0X1C, 0X1D, 0X1E, 0X1F, 0X20, 0X21, 0X22,
                                             0XFF, 0XFF, 0X23, 0X24, 0X25, 0X26, 0X27, 0X28, 0X29, 0X2A, 0X2B, 0X2C, 0X2D, 0X2E, 0X2F, 0X30, 0X31, 0X32, 0X33,
                                             0XFF, 0XFF, 0X34, 0X35, 0X36, 0X37, 0X38, 0X39, 0X3A, 0X3B, 0X3C, 0X3D, 0X3E, 0X3F, 0X40, 0X41, 0X42, 0X43, 0X44,
                                             0XFF, 0XFF, 0X45, 0X46, 0X47, 0X48, 0X49, 0X4A, 0X4B, 0X4C, 0X4D, 0X4E, 0X4F, 0X50, 0X51, 0X52, 0X53, 0X54, 0X55,
                                             0XFF, 0XFF, 0X56, 0X57, 0X58, 0X59, 0X5A, 0X5B, 0X5C, 0X5D, 0X5E, 0X5F, 0X60, 0X61, 0X62, 0X63, 0X64, 0X65, 0X66,
                                             0XFF, 0XFF, 0X67, 0X68, 0X69, 0X6A, 0X6B, 0X6C, 0X6D, 0X6E, 0X6F, 0X70, 0X71, 0X72, 0X73, 0X74, 0X75, 0X76, 0X77,
                                             0XFF, 0XFF, 0X78, 0X79, 0X7A, 0X7B, 0X7C, 0X7D, 0X7E, 0X7F, 0X80, 0X81, 0X82, 0X83, 0X84, 0X85, 0X86, 0X87, 0X88,
                                             0XFF, 0XFF, 0X89, 0X8A, 0X8B, 0X8C, 0X8D, 0X8E, 0X8F, 0X90, 0X91, 0X92, 0X93, 0X94, 0X95, 0X96, 0X97, 0X98, 0X99,
                                             0XFF, 0XFF, 0X9A, 0X9B, 0X9C, 0X9D, 0X9E, 0X9F, 0XA0, 0XA1, 0XA2, 0XA3, 0XA4, 0XA5, 0XA6, 0XA7, 0XA8, 0XA9, 0XAA,
                                             0XFF, 0XFF, 0XAB, 0XAC, 0XAD, 0XAE, 0XAF, 0XB0, 0XB1, 0XB2, 0XB3, 0XB4, 0XB5, 0XB6, 0XB7, 0XB8, 0XB9, 0XBA, 0XBB,
                                             0XFF, 0XFF, 0XBC, 0XBD, 0XBE, 0XBF, 0XC0, 0XC1, 0XC2, 0XC3, 0XC4, 0XC5, 0XC6, 0XC7, 0XC8, 0XC9, 0XCA, 0XCB, 0XCC,
                                             0XFF, 0XFF, 0XCD, 0XCE, 0XCF, 0XD0, 0XD1, 0XD2, 0XD3, 0XD4, 0XD5, 0XD6, 0XD7, 0XD8, 0XD9, 0XDA, 0XDB, 0XDC, 0XDD,
                                             0XFF, 0XFF, 0XDE, 0XDF, 0XE0, 0XE1, 0XE2, 0XE3, 0XE4, 0XE5, 0XE6, 0XE7, 0XE8, 0XE9, 0XEA, 0XEB, 0XEC, 0XED, 0XEE,
                                             0XFF, 0XFF, 0XEF, 0XF0, 0XF1, 0XF2, 0XF3, 0XF4, 0XF5, 0XF6, 0XF7, 0XF8, 0XF9, 0XFA, 0XFB, 0XFC, 0XFD, 0XFE, 0XFF,
                                             0XFF, 0XFF, 0X00, 0X01, 0X02, 0X03, 0X04, 0X05, 0X06, 0X07, 0X08, 0X09, 0X0A, 0X0B, 0X0C, 0X0D, 0X0E, 0X0F, 0X10,
                                             0XFF, 0XFF, 0X11, 0X12, 0X13, 0X14, 0X15, 0X16, 0X17, 0X18, 0X19, 0X1A, 0X1B, 0X1C, 0X1D, 0X1E, 0X1F, 0X20, 0X21,
                                             0XFF, 0XFF, 0X22, 0X23, 0X24, 0X25, 0X26, 0X27, 0X28, 0X29, 0X2A, 0X2B, 0X2C, 0X2D, 0X2E, 0X2F, 0X30, 0X31, 0X32,
                                             0XFF, 0XFF, 0X33, 0X34, 0X35, 0X36, 0X37, 0X38, 0X39, 0X3A, 0X3B, 0X3C, 0X3D, 0X3E, 0X3F, 0X40, 0X41, 0X42, 0X43,
                                             0XFF, 0XFF, 0X44, 0X45, 0X46, 0X47, 0X48, 0X49, 0X4A, 0X4B, 0X4C, 0X4D, 0X4E, 0X4F, 0X50, 0X51, 0X52, 0X53, 0X54,
                                             0XFF, 0XFF, 0X55, 0X56, 0X57, 0X58, 0X59, 0X5A, 0X5B, 0X5C, 0X5D, 0X5E, 0X5F, 0X60, 0X61, 0X62, 0X63, 0X64, 0X65,
                                             0XFF, 0XFF, 0X66, 0X67, 0X68, 0X69, 0X6A, 0X6B, 0X6C, 0X6D, 0X6E, 0X6F, 0X70, 0X71, 0X72, 0X73, 0X74, 0X75, 0X76,
                                             0XFF, 0XFF, 0X77, 0X78, 0X79, 0X80, 0X81, 0X82, 0X83, 0X84, 0X85, 0X86, 0X87, 0X88, 0X89, 0X90, 0X91, 0X92, 0X93,
                                             0XFF, 0XFF, 0X94, 0X95, 0X96, 0X97, 0X98, 0X99, 0X9A, 0X9B, 0X9C, 0X9D, 0X9E, 0X9F, 0XA0, 0XA1, 0XA2, 0XA3, 0XA4,
                                             0XFF, 0XFF, 0XA5, 0XA6, 0XA7, 0XA8, 0XA9, 0XAA, 0XAB, 0XAC, 0XAD, 0XAE, 0XAF, 0XB0, 0XB1, 0XB2, 0XB3, 0XB4, 0XB5,
                                             0XFF, 0XFF, 0XB6, 0XB7, 0XB8, 0XB9, 0XBA, 0XBB, 0XBC, 0XBD, 0XBE, 0XBF, 0XC0, 0XC1, 0XC2, 0XC3, 0XC4, 0XC5, 0XC6,
                                             0XFF, 0XFF, 0XC7, 0XC8, 0XC9, 0XCA, 0XCB, 0XCC, 0XCD, 0XCE, 0XCF, 0XD0, 0XD1, 0XD2, 0XD3, 0XD4, 0XD5, 0XD6, 0XD7,
                                             0XFF, 0XFF, 0XD8, 0XD9, 0XDA, 0XDB, 0XDC, 0XDD, 0XDE, 0XDF, 0XE0, 0XE1, 0XE2, 0XE3, 0XE4, 0XE5, 0XE6, 0XE7, 0XE8,
                                             0XFF, 0XFF, 0XE9, 0XEA, 0XEB, 0XEC, 0XED, 0XEE, 0XEF, 0XF0, 0XF1, 0XF2, 0XF3, 0XF4, 0XF5, 0XF6, 0XF7, 0XF8, 0XF9,
                                             0XFF, 0XFF, 0XFA, 0XFB, 0XFC, 0XFD, 0XFE, 0XFF, 0X00, 0X01, 0X02, 0X03, 0X04, 0X05, 0X06, 0X07, 0X08, 0X09, 0X0A,
                                             0XFF, 0XFF, 0X0B, 0X0C, 0X0D, 0X0E, 0X0F, 0X10, 0X11, 0X12, 0X13, 0X14, 0X15, 0X16, 0X17, 0X18, 0X19, 0X1A, 0X1B,
                                             0XFF, 0XFF, 0X1C, 0X1D, 0X1E, 0X1F, 0X20, 0X21, 0X22, 0X23, 0X24, 0X25, 0X26, 0X27, 0X28, 0X29, 0X2A, 0X2B, 0X2C,
                                             0XFF, 0XFF, 0X2D, 0X2E, 0X2F, 0X30, 0X31, 0X32, 0X33, 0X34, 0X35, 0X36, 0X37, 0X38, 0X39, 0X3A, 0X3B, 0X3C, 0X3D,
                                             0XFF, 0XFF, 0X3E, 0X3F, 0X40, 0X41, 0X42, 0X43, 0X44, 0X45, 0X46, 0X47, 0X48, 0X49, 0X4A, 0X4B, 0X4C, 0X4D, 0X4E,
                                             0XFF, 0XFF, 0X4F, 0X50, 0X51, 0X52, 0X53, 0X54, 0X55, 0X56, 0X57, 0X58, 0X59, 0X5A, 0X5B, 0X5C, 0X5D, 0X5E, 0X5F,
                                             0XFF, 0XFF, 0X60, 0X61, 0X62, 0X63, 0X64, 0X65, 0X66, 0X67, 0X68, 0X69, 0X6A, 0X6B, 0X6C, 0X6D, 0X6E, 0X6F, 0X70,
                                             0XFF, 0XFF, 0X71, 0X72, 0X73, 0X74, 0X75, 0X76, 0X77, 0X78, 0X79, 0X7A, 0X7B, 0X7C, 0X7D, 0X7E, 0X7F, 0X80, 0X81,
                                             0XFF, 0XFF, 0X82, 0X83, 0X84, 0X85, 0X86, 0X87, 0X88, 0X89, 0X8A, 0X8B, 0X8C, 0X8D, 0X8E, 0X8F, 0X90, 0X91, 0X92,
                                             0XFF, 0XFF, 0X93, 0X94, 0X95, 0X96, 0X97, 0X98, 0X99, 0X9A, 0X9B, 0X9C, 0X9D, 0X9E, 0X9F, 0XA0, 0XA1, 0XA2, 0XA3,
                                             0XFF, 0XFF, 0XA4, 0XA5, 0XA6, 0XA7, 0XA8, 0XA9, 0XAA, 0XAB, 0XAC, 0XAD, 0XAE, 0XAF, 0XB0, 0XB1, 0XB2, 0XB3, 0XB4,
                                             0XFF, 0XFF, 0XB5, 0XB6, 0XB7, 0XB8, 0XB9, 0XBA, 0XBB, 0XBC, 0XBD, 0XBE, 0XBF, 0XC0, 0XC1, 0XC2, 0XC3, 0XC4, 0XC5,
                                             0XFF, 0XFF, 0XC6, 0XC7, 0XC8, 0XC9, 0XCA, 0XCB, 0XCC, 0XCD, 0XCE, 0XCF, 0XD0, 0XD1, 0XD2, 0XD3, 0XD4, 0XD5, 0XD6,
                                             0XFF, 0XFF, 0XD7, 0XD8, 0XD9, 0XDA, 0XDB, 0XDC, 0XDD, 0XDE, 0XDF, 0XE0, 0XE1, 0XE2, 0XE3, 0XE4, 0XE5, 0XE6, 0XE7,
                                             0XFF, 0XFF, 0XE8, 0XE9, 0XEA, 0XEB, 0XEC, 0XED, 0XEE, 0XEF, 0XF0, 0XF1, 0XF2, 0XF3, 0XF4, 0XF5, 0XF6, 0XF7, 0XF8,
                                             0XFF, 0XFF, 0XF9, 0XFA, 0XFB, 0XFC, 0XFD, 0XFE, 0XFF, 0X00, 0X01, 0X02, 0X03, 0X04, 0X05, 0X06, 0X07, 0X08, 0X09,
                                             0XFF, 0XFF, 0X0A, 0X0B, 0X0C, 0X0D, 0X0E, 0X0F, 0X10, 0X11, 0X12, 0X13, 0X14, 0X15, 0X16, 0X17, 0X18, 0X19, 0X1A,
                                             0XFF, 0XFF, 0X1B, 0X1C, 0X1D, 0X1E, 0X1F, 0X20, 0X21, 0X22, 0X23, 0X24, 0X25, 0X26, 0X27, 0X28, 0X29, 0X2A, 0X2B,
                                             0XFF, 0XFF, 0X2C, 0X2D, 0X2E, 0X2F, 0X30, 0X31, 0X32, 0X33, 0X34, 0X35, 0X36, 0X37, 0X38, 0X39, 0X3A, 0X3B, 0X3C,
                                             0XFF, 0XFF, 0X3D, 0X3E, 0X3F, 0X40, 0X41, 0X42, 0X43, 0X44, 0X45, 0X46, 0X47, 0X48, 0X49, 0X4A, 0X4B, 0X4C, 0X4D,
                                             0XFF, 0XFF, 0X4E, 0X4F, 0X50, 0X51, 0X52, 0X53, 0X54, 0X55, 0X56, 0X57, 0X58, 0X59, 0X5A, 0X5B, 0X5C, 0X5D, 0X5E,
                                             0XFF, 0XFF, 0X5F, 0X60, 0X61, 0X62, 0X63, 0X64, 0X65, 0X66, 0X67, 0X68, 0X69, 0X6A, 0X6B, 0X6C, 0X6D, 0X6E, 0X6F,
                                             0XFF, 0XFF, 0X70, 0X71, 0X72, 0X73, 0X74, 0X75, 0X76, 0X77, 0X78, 0X79, 0X7A, 0X7B, 0X7C, 0X7D, 0X7E, 0X7F, 0X80,
                                             0XFF, 0XFF, 0X81, 0X82, 0X83, 0X84, 0X85, 0X86, 0X87, 0X88, 0X89, 0X8A, 0X8B, 0X8C, 0X8D, 0X8E, 0X8F, 0X90, 0X91,
                                             0XFF, 0XFF, 0X92, 0X93, 0X94, 0X95, 0X96, 0X97, 0X98, 0X99, 0X9A, 0X9B, 0X9C, 0X9D, 0X9E, 0X9F, 0XA0, 0XA1, 0XA2,
                                             0XFF, 0XFF, 0XA3, 0XA4, 0XA5, 0XA6, 0XA7, 0XA8, 0XA9, 0XAA, 0XAB, 0XAC, 0XAD, 0XAE, 0XAF, 0XB0, 0XB1, 0XB2, 0XB3,
                                             0XFF, 0XFF, 0XB4, 0XB5, 0XB6, 0XB7, 0XB8, 0XB9, 0XBA, 0XBB, 0XBC, 0XBD, 0XBE, 0XBF, 0XC0, 0XC1, 0XC2, 0XC3, 0XC4, };
        DigitalData expectedADCData = new DigitalData()
        {
            Header = new byte[] { 0XFF, 0XFF },
            Body = new byte[] { 0X01, 0X02, 0X03, 0X04, 0X05, 0X06, 0X07, 0X08, 0X09, 0X10, 0X11, 0X12, 0X13, 0X14, 0X15, 0X16, 0X17 }
        };

        byte[] expectedDigitalData = new byte[] {
        0X01, 0X02, 0X03, 0X04, 0X05, 0X06, 0X07, 0X08, 0X09, 0X10, 0X11, 0X12, 0X13, 0X14, 0X15, 0X16, 0X17,
                                0X18, 0X19, 0X20, 0X21, 0X22, 0X23, 0X24, 0X25, 0X26, 0X27, 0X28, 0X29, 0X30, 0X31, 0X32, 0X33, 0X34,
                                0X35, 0X36, 0X37, 0X38, 0X39, 0X3A, 0X3B, 0X3C, 0X3D, 0X3E, 0X3F, 0X40, 0X41, 0X42, 0X43, 0X44, 0X45,
                                0X46, 0X47, 0X48, 0X49, 0X4A, 0X4B, 0X4C, 0X4D, 0X4E, 0X4F, 0X50, 0X51, 0X52, 0X53, 0X54, 0X55, 0X56,
                                0X57, 0X58, 0X59, 0X5A, 0X5B, 0X5C, 0X5D, 0X5E, 0X5F, 0X60, 0X61, 0X62, 0X63, 0X64, 0X65, 0X66, 0X67,
                                0X68, 0X69, 0X6A, 0X6B, 0X6C, 0X6D, 0X6E, 0X6F, 0X70, 0X71, 0X72, 0X73, 0X74, 0X75, 0X76, 0X77, 0X78,
                                0X79, 0X7A, 0X7B, 0X7C, 0X7D, 0X7E, 0X7F, 0X80, 0X81, 0X82, 0X83, 0X84, 0X85, 0X86, 0X87, 0X88, 0X89,
                                0X8A, 0X8B, 0X8C, 0X8D, 0X8E, 0X8F, 0X90, 0X91, 0X92, 0X93, 0X94, 0X95, 0X96, 0X97, 0X98, 0X99, 0X9A,
                                0X9B, 0X9C, 0X9D, 0X9E, 0X9F, 0XA0, 0XA1, 0XA2, 0XA3, 0XA4, 0XA5, 0XA6, 0XA7, 0XA8, 0XA9, 0XAA, 0XAB,
                                0XAC, 0XAD, 0XAE, 0XAF, 0XB0, 0XB1, 0XB2, 0XB3, 0XB4, 0XB5, 0XB6, 0XB7, 0XB8, 0XB9, 0XBA, 0XBB, 0XBC,
                                0XBD, 0XBE, 0XBF, 0XC0, 0XC1, 0XC2, 0XC3, 0XC4, 0XC5, 0XC6, 0XC7, 0XC8, 0XC9, 0XCA, 0XCB, 0XCC, 0XCD,
                                0XCE, 0XCF, 0XD0, 0XD1, 0XD2, 0XD3, 0XD4, 0XD5, 0XD6, 0XD7, 0XD8, 0XD9, 0XDA, 0XDB, 0XDC, 0XDD, 0XDE,
                                0XDF, 0XE0, 0XE1, 0XE2, 0XE3, 0XE4, 0XE5, 0XE6, 0XE7, 0XE8, 0XE9, 0XEA, 0XEB, 0XEC, 0XED, 0XEE, 0XEF,
                                0XF0, 0XF1, 0XF2, 0XF3, 0XF4, 0XF5, 0XF6, 0XF7, 0XF8, 0XF9, 0XFA, 0XFB, 0XFC, 0XFD, 0XFE, 0XFF, 0X00,
                                0X01, 0X02, 0X03, 0X04, 0X05, 0X06, 0X07, 0X08, 0X09, 0X0A, 0X0B, 0X0C, 0X0D, 0X0E, 0X0F, 0X10, 0X11,
                                0X12, 0X13, 0X14, 0X15, 0X16, 0X17, 0X18, 0X19, 0X1A, 0X1B, 0X1C, 0X1D, 0X1E, 0X1F, 0X20, 0X21, 0X22,
                                0X23, 0X24, 0X25, 0X26, 0X27, 0X28, 0X29, 0X2A, 0X2B, 0X2C, 0X2D, 0X2E, 0X2F, 0X30, 0X31, 0X32, 0X33,
                                0X34, 0X35, 0X36, 0X37, 0X38, 0X39, 0X3A, 0X3B, 0X3C, 0X3D, 0X3E, 0X3F, 0X40, 0X41, 0X42, 0X43, 0X44,
                                0X45, 0X46, 0X47, 0X48, 0X49, 0X4A, 0X4B, 0X4C, 0X4D, 0X4E, 0X4F, 0X50, 0X51, 0X52, 0X53, 0X54, 0X55,
                                0X56, 0X57, 0X58, 0X59, 0X5A, 0X5B, 0X5C, 0X5D, 0X5E, 0X5F, 0X60, 0X61, 0X62, 0X63, 0X64, 0X65, 0X66,
                                0X67, 0X68, 0X69, 0X6A, 0X6B, 0X6C, 0X6D, 0X6E, 0X6F, 0X70, 0X71, 0X72, 0X73, 0X74, 0X75, 0X76, 0X77,
                                0X78, 0X79, 0X7A, 0X7B, 0X7C, 0X7D, 0X7E, 0X7F, 0X80, 0X81, 0X82, 0X83, 0X84, 0X85, 0X86, 0X87, 0X88,
                                0X89, 0X8A, 0X8B, 0X8C, 0X8D, 0X8E, 0X8F, 0X90, 0X91, 0X92, 0X93, 0X94, 0X95, 0X96, 0X97, 0X98, 0X99,
                                0X9A, 0X9B, 0X9C, 0X9D, 0X9E, 0X9F, 0XA0, 0XA1, 0XA2, 0XA3, 0XA4, 0XA5, 0XA6, 0XA7, 0XA8, 0XA9, 0XAA,
                                0XAB, 0XAC, 0XAD, 0XAE, 0XAF, 0XB0, 0XB1, 0XB2, 0XB3, 0XB4, 0XB5, 0XB6, 0XB7, 0XB8, 0XB9, 0XBA, 0XBB,
                                0XBC, 0XBD, 0XBE, 0XBF, 0XC0, 0XC1, 0XC2, 0XC3, 0XC4, 0XC5, 0XC6, 0XC7, 0XC8, 0XC9, 0XCA, 0XCB, 0XCC,
                                0XCD, 0XCE, 0XCF, 0XD0, 0XD1, 0XD2, 0XD3, 0XD4, 0XD5, 0XD6, 0XD7, 0XD8, 0XD9, 0XDA, 0XDB, 0XDC, 0XDD,
                                0XDE, 0XDF, 0XE0, 0XE1, 0XE2, 0XE3, 0XE4, 0XE5, 0XE6, 0XE7, 0XE8, 0XE9, 0XEA, 0XEB, 0XEC, 0XED, 0XEE,
                                0XEF, 0XF0, 0XF1, 0XF2, 0XF3, 0XF4, 0XF5, 0XF6, 0XF7, 0XF8, 0XF9, 0XFA, 0XFB, 0XFC, 0XFD, 0XFE, 0XFF,
                                0X00, 0X01, 0X02, 0X03, 0X04, 0X05, 0X06, 0X07, 0X08, 0X09, 0X0A, 0X0B, 0X0C, 0X0D, 0X0E, 0X0F, 0X10,
                                0X11, 0X12, 0X13, 0X14, 0X15, 0X16, 0X17, 0X18, 0X19, 0X1A, 0X1B, 0X1C, 0X1D, 0X1E, 0X1F, 0X20, 0X21,
                                0X22, 0X23, 0X24, 0X25, 0X26, 0X27, 0X28, 0X29, 0X2A, 0X2B, 0X2C, 0X2D, 0X2E, 0X2F, 0X30, 0X31, 0X32,
                                0X33, 0X34, 0X35, 0X36, 0X37, 0X38, 0X39, 0X3A, 0X3B, 0X3C, 0X3D, 0X3E, 0X3F, 0X40, 0X41, 0X42, 0X43,
                                0X44, 0X45, 0X46, 0X47, 0X48, 0X49, 0X4A, 0X4B, 0X4C, 0X4D, 0X4E, 0X4F, 0X50, 0X51, 0X52, 0X53, 0X54,
                                0X55, 0X56, 0X57, 0X58, 0X59, 0X5A, 0X5B, 0X5C, 0X5D, 0X5E, 0X5F, 0X60, 0X61, 0X62, 0X63, 0X64, 0X65,
                                0X66, 0X67, 0X68, 0X69, 0X6A, 0X6B, 0X6C, 0X6D, 0X6E, 0X6F, 0X70, 0X71, 0X72, 0X73, 0X74, 0X75, 0X76,
                                0X77, 0X78, 0X79, 0X80, 0X81, 0X82, 0X83, 0X84, 0X85, 0X86, 0X87, 0X88, 0X89, 0X90, 0X91, 0X92, 0X93,
                                0X94, 0X95, 0X96, 0X97, 0X98, 0X99, 0X9A, 0X9B, 0X9C, 0X9D, 0X9E, 0X9F, 0XA0, 0XA1, 0XA2, 0XA3, 0XA4,
                                0XA5, 0XA6, 0XA7, 0XA8, 0XA9, 0XAA, 0XAB, 0XAC, 0XAD, 0XAE, 0XAF, 0XB0, 0XB1, 0XB2, 0XB3, 0XB4, 0XB5,
                                0XB6, 0XB7, 0XB8, 0XB9, 0XBA, 0XBB, 0XBC, 0XBD, 0XBE, 0XBF, 0XC0, 0XC1, 0XC2, 0XC3, 0XC4, 0XC5, 0XC6,
                                0XC7, 0XC8, 0XC9, 0XCA, 0XCB, 0XCC, 0XCD, 0XCE, 0XCF, 0XD0, 0XD1, 0XD2, 0XD3, 0XD4, 0XD5, 0XD6, 0XD7,
                                0XD8, 0XD9, 0XDA, 0XDB, 0XDC, 0XDD, 0XDE, 0XDF, 0XE0, 0XE1, 0XE2, 0XE3, 0XE4, 0XE5, 0XE6, 0XE7, 0XE8,
                                0XE9, 0XEA, 0XEB, 0XEC, 0XED, 0XEE, 0XEF, 0XF0, 0XF1, 0XF2, 0XF3, 0XF4, 0XF5, 0XF6, 0XF7, 0XF8, 0XF9,
                                0XFA, 0XFB, 0XFC, 0XFD, 0XFE, 0XFF, 0X00, 0X01, 0X02, 0X03, 0X04, 0X05, 0X06, 0X07, 0X08, 0X09, 0X0A,
                                0X0B, 0X0C, 0X0D, 0X0E, 0X0F, 0X10, 0X11, 0X12, 0X13, 0X14, 0X15, 0X16, 0X17, 0X18, 0X19, 0X1A, 0X1B,
                                0X1C, 0X1D, 0X1E, 0X1F, 0X20, 0X21, 0X22, 0X23, 0X24, 0X25, 0X26, 0X27, 0X28, 0X29, 0X2A, 0X2B, 0X2C,
                                0X2D, 0X2E, 0X2F, 0X30, 0X31, 0X32, 0X33, 0X34, 0X35, 0X36, 0X37, 0X38, 0X39, 0X3A, 0X3B, 0X3C, 0X3D,
                                0X3E, 0X3F, 0X40, 0X41, 0X42, 0X43, 0X44, 0X45, 0X46, 0X47, 0X48, 0X49, 0X4A, 0X4B, 0X4C, 0X4D, 0X4E,
                                0X4F, 0X50, 0X51, 0X52, 0X53, 0X54, 0X55, 0X56, 0X57, 0X58, 0X59, 0X5A, 0X5B, 0X5C, 0X5D, 0X5E, 0X5F,
                                0X60, 0X61, 0X62, 0X63, 0X64, 0X65, 0X66, 0X67, 0X68, 0X69, 0X6A, 0X6B, 0X6C, 0X6D, 0X6E, 0X6F, 0X70,
                                0X71, 0X72, 0X73, 0X74, 0X75, 0X76, 0X77, 0X78, 0X79, 0X7A, 0X7B, 0X7C, 0X7D, 0X7E, 0X7F, 0X80, 0X81,
                                0X82, 0X83, 0X84, 0X85, 0X86, 0X87, 0X88, 0X89, 0X8A, 0X8B, 0X8C, 0X8D, 0X8E, 0X8F, 0X90, 0X91, 0X92,
                                0X93, 0X94, 0X95, 0X96, 0X97, 0X98, 0X99, 0X9A, 0X9B, 0X9C, 0X9D, 0X9E, 0X9F, 0XA0, 0XA1, 0XA2, 0XA3,
                                0XA4, 0XA5, 0XA6, 0XA7, 0XA8, 0XA9, 0XAA, 0XAB, 0XAC, 0XAD, 0XAE, 0XAF, 0XB0, 0XB1, 0XB2, 0XB3, 0XB4,
                                0XB5, 0XB6, 0XB7, 0XB8, 0XB9, 0XBA, 0XBB, 0XBC, 0XBD, 0XBE, 0XBF, 0XC0, 0XC1, 0XC2, 0XC3, 0XC4, 0XC5,
                                0XC6, 0XC7, 0XC8, 0XC9, 0XCA, 0XCB, 0XCC, 0XCD, 0XCE, 0XCF, 0XD0, 0XD1, 0XD2, 0XD3, 0XD4, 0XD5, 0XD6,
                                0XD7, 0XD8, 0XD9, 0XDA, 0XDB, 0XDC, 0XDD, 0XDE, 0XDF, 0XE0, 0XE1, 0XE2, 0XE3, 0XE4, 0XE5, 0XE6, 0XE7,
                                0XE8, 0XE9, 0XEA, 0XEB, 0XEC, 0XED, 0XEE, 0XEF, 0XF0, 0XF1, 0XF2, 0XF3, 0XF4, 0XF5, 0XF6, 0XF7, 0XF8,
                                0XF9, 0XFA, 0XFB, 0XFC, 0XFD, 0XFE, 0XFF, 0X00, 0X01, 0X02, 0X03, 0X04, 0X05, 0X06, 0X07, 0X08, 0X09,
                                0X0A, 0X0B, 0X0C, 0X0D, 0X0E, 0X0F, 0X10, 0X11, 0X12, 0X13, 0X14, 0X15, 0X16, 0X17, 0X18, 0X19, 0X1A,
                                0X1B, 0X1C, 0X1D, 0X1E, 0X1F, 0X20, 0X21, 0X22, 0X23, 0X24, 0X25, 0X26, 0X27, 0X28, 0X29, 0X2A, 0X2B,
                                0X2C, 0X2D, 0X2E, 0X2F, 0X30, 0X31, 0X32, 0X33, 0X34, 0X35, 0X36, 0X37, 0X38, 0X39, 0X3A, 0X3B, 0X3C,
                                0X3D, 0X3E, 0X3F, 0X40, 0X41, 0X42, 0X43, 0X44, 0X45, 0X46, 0X47, 0X48, 0X49, 0X4A, 0X4B, 0X4C, 0X4D,
                                0X4E, 0X4F, 0X50, 0X51, 0X52, 0X53, 0X54, 0X55, 0X56, 0X57, 0X58, 0X59, 0X5A, 0X5B, 0X5C, 0X5D, 0X5E,
                                0X5F, 0X60, 0X61, 0X62, 0X63, 0X64, 0X65, 0X66, 0X67, 0X68, 0X69, 0X6A, 0X6B, 0X6C, 0X6D, 0X6E, 0X6F,
                                0X70, 0X71, 0X72, 0X73, 0X74, 0X75, 0X76, 0X77, 0X78, 0X79, 0X7A, 0X7B, 0X7C, 0X7D, 0X7E, 0X7F, 0X80,
                                0X81, 0X82, 0X83, 0X84, 0X85, 0X86, 0X87, 0X88, 0X89, 0X8A, 0X8B, 0X8C, 0X8D, 0X8E, 0X8F, 0X90, 0X91,
                                0X92, 0X93, 0X94, 0X95, 0X96, 0X97, 0X98, 0X99, 0X9A, 0X9B, 0X9C, 0X9D, 0X9E, 0X9F, 0XA0, 0XA1, 0XA2,
                                0XA3, 0XA4, 0XA5, 0XA6, 0XA7, 0XA8, 0XA9, 0XAA, 0XAB, 0XAC, 0XAD, 0XAE, 0XAF, 0XB0, 0XB1, 0XB2, 0XB3,
                                0XB4, 0XB5, 0XB6, 0XB7, 0XB8, 0XB9, 0XBA, 0XBB, 0XBC, 0XBD, 0XBE, 0XBF, 0XC0, 0XC1, 0XC2, 0XC3, 0XC4};

        private byte[] stimCommand = new byte[]
        {
            0XFF, 0XFA, 0X05, 0X00, 0X11, 0X22, 0X33, 0X44, 0X55, 0X66, 0X77, 0X88, 0X99, 0XAA, 0XBB, 0XCC, 0XDD, 0XEE, 0XFF
        };

        private void ConnectToMobile(string ip4thSection)
        {
            var x = "192.168.3." + ip4thSection;
            _connection = new TCPConnection(x, 5000);
            _connection.StartConnection();
        }

        [TestInitialize]
        public void Init()
        {
            _connection = new TCPConnection("192.168.3.128", 5000);
            _connection.StartConnection();
        }

        [TestMethod()]
        public void CheckIfADeviceIsConnectedInIPRangeOf192_198_3()
        {
            Assert.IsTrue(_connection.IsConnected());
        }

        [TestMethod()]
        public void ExtractCurrentSampleFromTheReceivedPacket()
        {
            SerialData stimSample = _connection.ExtractCurrentSample(stimCommandPacket) as SerialData;
            Assert.AreEqual(expectedStimCommand, stimSample);

            SerialData headBoxSample = _connection.ExtractCurrentSample(headBoxCommandPacket) as SerialData;
            Assert.AreEqual(expectedHeadBoxCommand, headBoxSample);

            DigitalData dataSample = _connection.ExtractCurrentSample(dataPacket) as DigitalData;
            Assert.AreEqual(expectedADCData, dataSample);
        }

        [TestMethod()]
        public void GetADCDigitalDataFromSocket()
        {
            ConnectToMobile("124");
            byte[] packet = _connection.ReadSocketData();
            CollectionAssert.AreEqual(dataPacket, packet);
        }

        [TestMethod()]
        public void GetADCDigitalDataFromSocketAndExtractADigitalDataFromIt()
        {
            ConnectToMobile("124");
            byte[] packet = _connection.ReadSocketData();
            DigitalData digitalDataSample = _connection.ExtractCurrentSample(packet) as DigitalData;
            Assert.AreEqual(expectedADCData, digitalDataSample);
        }

        [TestMethod()]
        public void GetHeadBoxSerialDataFromSocket()
        {
            byte[] packet = _connection.ReadSocketData();
            CollectionAssert.AreEqual(headBoxCommandPacket, packet);
        }

        [TestMethod()]
        public void GetHeadBoxSerialDataFromSocketAndExtractAHeadBoxCommandFromIt()
        {
            byte[] packet = _connection.ReadSocketData();
            SerialData headBoxSample = _connection.ExtractCurrentSample(packet) as SerialData;
            Assert.AreEqual(expectedHeadBoxCommand, headBoxSample);
        }

        [TestMethod()]
        public void GetStimSerialDataFromSocket()
        {
            byte[] packet = _connection.ReadSocketData();
            CollectionAssert.AreEqual(stimCommandPacket, packet);
        }

        [TestMethod()]
        public void GetStimSerialDataFromSocketAndExtractAStimCommandFromIt()
        {
            byte[] packet = _connection.ReadSocketData();
            SerialData stimSample = _connection.ExtractCurrentSample(packet) as SerialData;
            Assert.AreEqual(expectedStimCommand, stimSample);
        }

        [TestMethod()]
        public void SendACommandAsAByteArrayThroughTheSocket()
        {
            //ConnectToMobile();
            bool exceptionIsThrown = false;
            try
            {
                _connection.SendCommand(stimCommand);
            }
            catch (TCPSendMessageException)
            {
                exceptionIsThrown = true;
            }

            Assert.IsFalse(exceptionIsThrown);
        }

        [TestMethod]
        public void ExtractTheRealDigitalDataFromThePacket()
        {
            byte[] digitalData = _connection.ExtractDigitalDataFromPacket(dataPacket, 70);
            CollectionAssert.AreEqual(expectedDigitalData, digitalData);
        }

        [TestMethod]
        public void GetPacketsInAThread()
        {
            ConnectToMobile("124");
            Task readSocket = Task.Factory.StartNew(() => _connection.ReadSocketDataBuffer());
            byte[] digitalDataBuffer = _connection.ReadDigitalDataBuffer();
            if (digitalDataBuffer.GetLength(0) > 0 && digitalDataBuffer.GetLength(1) > 0)
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string filePath = Path.Combine(desktopPath, "DigitalDataBuffer.txt");
                Write2DByteArrayToFile(digitalDataBuffer, filePath);
            }
        }

        static void Write2DByteArrayToFile(byte[] byteArray, string filePath)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(filePath, FileMode.Create)))
            {
                int rows = byteArray.GetLength(0);
                int cols = byteArray.GetLength(1);

                writer.Write(rows);
                writer.Write(cols);

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        writer.Write(byteArray[i]);
                    }
                }
            }
        }
    }
}
