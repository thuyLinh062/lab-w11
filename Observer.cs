//Họ và Tên: Trần Thùy Linh
//MSSV: 202418935
using System;
using System.Collections.Generic;

namespace CampusSafetyHub;

public interface IAlertObserver
{
    void Update(AlertEvent alertEvent);
}

public sealed class SensorMonitor
{
    private readonly List<IAlertObserver> observers = new();

    public void Attach(IAlertObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        if (!observers.Contains(observer))
        {
            observers.Add(observer);
        }
    }

    public void Detach(IAlertObserver observer)
    {
        observers.Remove(observer);
    }

    public void CheckSensor(string sensorName, ISensor sensor)
    {
        ArgumentNullException.ThrowIfNull(sensor);

        double value = sensor.ReadValue();
        string? severity = value switch
        {
            >= 70 => "CRITICAL",
            >= 50 => "WARNING",
            _ => null
        };

        if (severity is null)
        {
            return;
        }

        var alertEvent = new AlertEvent(
            sensorName,
            value,
            sensor.GetUnit(),
            severity,
            DateTime.Now);

        Notify(alertEvent);
    }

    private void Notify(AlertEvent alertEvent)
    {
        foreach (IAlertObserver observer in observers.ToArray())
        {
            try
            {
                observer.Update(alertEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MONITOR ERROR] Observer {observer.GetType().Name} gặp lỗi: {ex.Message}");
            }
        }
    }
}

public sealed class DashboardObserver : IAlertObserver
{
    public void Update(AlertEvent alertEvent)
    {
        Console.WriteLine(
            $"[DASHBOARD] {alertEvent.SensorName}: " +
            $"{alertEvent.Value:F1}{alertEvent.Unit} " +
            $"[{alertEvent.Severity}]");
    }
}

public sealed class EmailAlertObserver : IAlertObserver
{
    public void Update(AlertEvent alertEvent)
    {
        Console.WriteLine(
            $"[EMAIL] Gửi cảnh báo {alertEvent.Severity} cho {alertEvent.SensorName} " +
            $"({alertEvent.Value:F1}{alertEvent.Unit}) lúc {alertEvent.OccurredAt:HH:mm:ss}");
    }
}

public sealed class AuditLogObserver : IAlertObserver
{
    public void Update(AlertEvent alertEvent)
    {
        Console.WriteLine(
            $"[AUDIT LOG] {alertEvent.OccurredAt:yyyy-MM-dd HH:mm:ss} | " +
            $"{alertEvent.SensorName} | {alertEvent.Value:F1} {alertEvent.Unit} | {alertEvent.Severity}");
    }
}