// Decompiled with JetBrains decompiler
// Type: serialtoip.CrossThreadComm
// Assembly: SerialToIPGUI, Version=1.9.5866.19643, Culture=neutral, PublicKeyToken=null
// MVID: B1D67A1F-B8BA-49F3-B87A-0CFB4BE0BA84
// Assembly location: C:\Users\Alex\Downloads\SerialToIPGUI_v1.9_2016-01-23\SerialToIPGUI.exe

namespace serialtoip
{
  public class CrossThreadComm
  {
    public enum State
    {
      start,
      connect,
      disconnect,
      stop,
      terminate,
    }

    public delegate void TraceCb(object obj);

    public delegate void UpdateState(object obj, CrossThreadComm.State state);

    public delegate void UpdateRXTX(object obj, int bytesFromSerial, int bytesToSerial);
  }
}
