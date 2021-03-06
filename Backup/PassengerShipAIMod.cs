﻿// Decompiled with JetBrains decompiler
// Type: ImprovedPublicTransport.PassengerShipAIMod
// Assembly: ImprovedPublicTransport, Version=1.0.6177.17409, Culture=neutral, PublicKeyToken=null
// MVID: 76F370C5-F40B-41AE-AA9D-1E3F87E934D3
// Assembly location: C:\Games\Steam\steamapps\workshop\content\255710\424106600\ImprovedPublicTransport.dll

using ColossalFramework;
using ImprovedPublicTransport.Detour;
using UnityEngine;

namespace ImprovedPublicTransport
{
  public class PassengerShipAIMod : PassengerShipAI
  {
    private static bool _isDeployed;
    private static PassengerShipAIMod.LoadPassengersCallback LoadPassengers;
    private static PassengerShipAIMod.UnloadPassengersCallback UnloadPassengers;
    private static Redirection<PassengerShipAI, PassengerShipAIMod> _redirectionArriveAtTarget;
    private static Redirection<PassengerShipAI, BusAIMod> _redirectionCanLeave;

    public static void Init()
    {
      if (PassengerShipAIMod._isDeployed)
        return;
      PassengerShipAIMod.LoadPassengers = (PassengerShipAIMod.LoadPassengersCallback) Utils.CreateDelegate<PassengerShipAI, PassengerShipAIMod.LoadPassengersCallback>("LoadPassengers", (object) null);
      PassengerShipAIMod.UnloadPassengers = (PassengerShipAIMod.UnloadPassengersCallback) Utils.CreateDelegate<PassengerShipAI, PassengerShipAIMod.UnloadPassengersCallback>("UnloadPassengers", (object) null);
      PassengerShipAIMod._redirectionArriveAtTarget = new Redirection<PassengerShipAI, PassengerShipAIMod>("ArriveAtTarget");
      PassengerShipAIMod._redirectionCanLeave = new Redirection<PassengerShipAI, BusAIMod>("CanLeave");
      PassengerShipAIMod._isDeployed = true;
    }

    public static void Deinit()
    {
      if (!PassengerShipAIMod._isDeployed)
        return;
      PassengerShipAIMod.LoadPassengers = (PassengerShipAIMod.LoadPassengersCallback) null;
      PassengerShipAIMod.UnloadPassengers = (PassengerShipAIMod.UnloadPassengersCallback) null;
      PassengerShipAIMod._redirectionArriveAtTarget.Revert();
      PassengerShipAIMod._redirectionArriveAtTarget = (Redirection<PassengerShipAI, PassengerShipAIMod>) null;
      PassengerShipAIMod._redirectionCanLeave.Revert();
      PassengerShipAIMod._redirectionCanLeave = (Redirection<PassengerShipAI, BusAIMod>) null;
      PassengerShipAIMod._isDeployed = false;
    }

    private bool ArriveAtTarget(ushort vehicleID, ref Vehicle data)
    {
      if ((int) data.m_targetBuilding == 0 || (data.m_flags & Vehicle.Flags.DummyTraffic) != ~(Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding | Vehicle.Flags.LeftHandDrive))
      {
        Singleton<VehicleManager>.instance.ReleaseVehicle(vehicleID);
        return true;
      }
      ushort targetBuilding = data.m_targetBuilding;
      ushort nextStop = 0;
      if ((int) data.m_transportLine != 0)
        nextStop = TransportLine.GetNextStop(data.m_targetBuilding);
      else if ((data.m_flags & (Vehicle.Flags.Importing | Vehicle.Flags.Exporting)) != ~(Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding | Vehicle.Flags.LeftHandDrive))
      {
        nextStop = TransportLine.GetNextStop(data.m_targetBuilding);
        Vector3 lastFramePosition = data.GetLastFramePosition();
        byte max;
        if ((double) Mathf.Max(Mathf.Abs(lastFramePosition.x), Mathf.Abs(lastFramePosition.z)) > 4800.0 && PanelExtenderLine.CountWaitingPassengers(targetBuilding, nextStop, out max) == 0)
          nextStop = (ushort) 0;
      }
      ushort transferSize1 = data.m_transferSize;
      PassengerShipAIMod.UnloadPassengers(data.Info.m_vehicleAI as PassengerShipAI, vehicleID, ref data, targetBuilding, nextStop);
      ushort num1 = (ushort) ((uint) transferSize1 - (uint) data.m_transferSize);
      VehicleManagerMod.m_cachedVehicleData[(int) vehicleID].LastStopGonePassengers = (int) num1;
      VehicleManagerMod.m_cachedVehicleData[(int) vehicleID].CurrentStop = targetBuilding;
      NetManagerMod.m_cachedNodeData[(int) targetBuilding].PassengersOut += (int) num1;
      if ((int) nextStop == 0)
      {
        data.m_waitCounter = (byte) 0;
        data.m_flags |= Vehicle.Flags.WaitingLoading;
      }
      else
      {
        data.m_targetBuilding = nextStop;
        if (!this.StartPathFind(vehicleID, ref data))
          return true;
        ushort transferSize2 = data.m_transferSize;
        PassengerShipAIMod.LoadPassengers(data.Info.m_vehicleAI as PassengerShipAI, vehicleID, ref data, targetBuilding, nextStop);
        ushort num2 = (ushort) ((uint) data.m_transferSize - (uint) transferSize2);
        int ticketPrice = data.Info.m_vehicleAI.GetTicketPrice(vehicleID, ref data);
        VehicleManagerMod.m_cachedVehicleData[(int) vehicleID].Add((int) num2, ticketPrice);
        NetManagerMod.m_cachedNodeData[(int) targetBuilding].PassengersIn += (int) num2;
        data.m_flags &= Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding | Vehicle.Flags.LeftHandDrive;
        data.m_flags |= Vehicle.Flags.Stopped;
        data.m_waitCounter = (byte) 0;
      }
      return false;
    }

    private delegate void LoadPassengersCallback(PassengerShipAI ai, ushort vehicleID, ref Vehicle data, ushort currentStop, ushort nextStop);

    private delegate void UnloadPassengersCallback(PassengerShipAI ai, ushort vehicleID, ref Vehicle data, ushort currentStop, ushort nextStop);
  }
}
