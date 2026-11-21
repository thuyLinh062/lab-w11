//Họ và Tên: Trần Thùy Linh
//MSSV: 202418935
using System;
using System.Collections.Generic;

namespace CampusSafetyHub;

public interface IEmergencyMediator
{
    void Notify(object sender, string eventCode, AlertEvent? alertEvent = null);
}

public sealed class Alarm
{
    public void Activate() => Console.WriteLine("[ALARM] Còi & đèn báo động khẩn cấp BẬT!");
    public void Deactivate() => Console.WriteLine("[ALARM] Còi & đèn báo động TẮT.");
}

public sealed class DoorController
{
    public void UnlockEmergencyExits() => Console.WriteLine("[DOOR] Mở khóa toàn bộ cửa thoát hiểm phòng máy.");
    public void LockNormalDoors() => Console.WriteLine("[DOOR] Đóng/khóa cửa ngăn cháy khu vực.");
}

public sealed class VentilationController
{
    public void EnableSmokeExhaust() => Console.WriteLine("[VENTILATION] Bật quạt hút khói và ngắt thông gió tuần hoàn.");
    public void SetNormalMode() => Console.WriteLine("[VENTILATION] Chuyển về chế độ thông gió bình thường.");
}

public sealed class EmergencyMediator : IEmergencyMediator
{
    private readonly Alarm alarm;
    private readonly DoorController doorController;
    private readonly VentilationController ventilationController;

    public EmergencyMediator(
        Alarm alarm,
        DoorController doorController,
        VentilationController ventilationController)
    {
        this.alarm = alarm ?? throw new ArgumentNullException(nameof(alarm));
        this.doorController = doorController ?? throw new ArgumentNullException(nameof(doorController));
        this.ventilationController = ventilationController ?? throw new ArgumentNullException(nameof(ventilationController));
    }

    public void Notify(object sender, string eventCode, AlertEvent? alertEvent = null)
    {
        Console.WriteLine($"[MEDIATOR] Nhận sự kiện '{eventCode}' từ {sender.GetType().Name}.");

        if (eventCode == "FIRE_DETECTED")
        {
            alarm.Activate();
            doorController.UnlockEmergencyExits();
            ventilationController.EnableSmokeExhaust();
        }
        else if (eventCode == "RESET_SAFE")
        {
            alarm.Deactivate();
            ventilationController.SetNormalMode();
        }
    }
}

public sealed class EmergencyBridgeObserver : IAlertObserver
{
    private readonly IEmergencyMediator mediator;

    public EmergencyBridgeObserver(IEmergencyMediator mediator)
    {
        this.mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    public void Update(AlertEvent alertEvent)
    {
        if (alertEvent.Severity == "CRITICAL")
        {
            mediator.Notify(this, "FIRE_DETECTED", alertEvent);
        }
    }
}