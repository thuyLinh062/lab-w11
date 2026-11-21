
using System;
using System.Collections.Generic;
namespace CampusSafetyHub;

internal static class Program
{
    private static int passed = 0;
    private static int failed = 0;

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("========================================");
        Console.WriteLine("     CAMPUS SAFETY HUB - TEST PROGRAM");
        Console.WriteLine("========================================");

        // 1. KIỂM THỬ ADAPTER
        RunTest("ADAPTER", TestAdapterZero);
        RunTest("ADAPTER", TestAdapterConversion);
        RunTest("ADAPTER", TestAdapterMaximum);
        RunTest("ADAPTER", TestAdapterLiveReference);
        RunTest("ADAPTER", TestAdapterNull);

        // 2. KIỂM THỬ OBSERVER
        RunTest("OBSERVER", TestNormalValue);
        RunTest("OBSERVER", TestWarningThreshold);
        RunTest("OBSERVER", TestCriticalThreshold);
        RunTest("OBSERVER", TestDuplicateObserver);
        RunTest("OBSERVER", TestDetachObserver);
        RunTest("OBSERVER", TestMultipleObservers);
        RunTest("OBSERVER", TestObserverException);
        RunTest("OBSERVER", TestDetachDuringNotification);
        RunTest("OBSERVER", TestNullSensor);

        // 3. KIỂM THỬ MEDIATOR
        RunTest("MEDIATOR", TestFireDetected);
        RunTest("MEDIATOR", TestWarningDoesNotActivateEmergency);
        RunTest("MEDIATOR", TestResetSafe);
        RunTest("MEDIATOR", TestMediatorNullDependencies);

        // TỔNG KẾT
        Console.WriteLine("\n========================================");
        Console.WriteLine("             TEST SUMMARY");
        Console.WriteLine("========================================");
        Console.WriteLine($"Total tests : {passed + failed}");
        Console.WriteLine($"Passed      : {passed}");
        Console.WriteLine($"Failed      : {failed}");
        Console.WriteLine("========================================");

        Console.WriteLine(failed == 0
            ? "ALL TESTS PASSED!"
            : "SOME TESTS FAILED!");
    }

    // ==========================================
    // HÀM HỖ TRỢ KIỂM THỬ
    // ==========================================

    static void RunTest(string group, Action test)
    {
        try
        {
            test();
            passed++;
            Console.WriteLine($"[PASS] [{group}] {test.Method.Name}");
        }
        catch (Exception ex)
        {
            failed++;
            Console.WriteLine($"[FAIL] [{group}] {test.Method.Name}");
            Console.WriteLine($"       Reason: {ex.Message}");
        }
    }

    static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new Exception(
                $"Expected: {expected}, Actual: {actual}");
        }
    }

    static void AssertTrue(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    static void AssertContains(string expected, string actual)
    {
        if (!actual.Contains(expected))
        {
            throw new Exception(
                $"Output does not contain: {expected}");
        }
    }

    static void AssertThrows<T>(Action action)
        where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }

        throw new Exception(
            $"Expected exception: {typeof(T).Name}");
    }

    static string CaptureOutput(Action action)
    {
        var original = Console.Out;
        using var writer = new System.IO.StringWriter();

        try
        {
            Console.SetOut(writer);
            action();
            return writer.ToString();
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    // ==========================================
    // TEST DOUBLES
    // ==========================================

    // Cảm biến giả để kiểm thử SensorMonitor
    private sealed class FakeSensor : ISensor
    {
        private readonly double value;

        public FakeSensor(double value)
        {
            this.value = value;
        }

        public double ReadValue() => value;

        public string GetUnit() => "% smoke";
    }

    // Observer ghi nhận số lần nhận cảnh báo
    private sealed class RecordingObserver : IAlertObserver
    {
        public int CallCount { get; private set; }

        public AlertEvent? LastAlert { get; private set; }

        public void Update(AlertEvent alertEvent)
        {
            CallCount++;
            LastAlert = alertEvent;
        }
    }

    // Observer cố tình phát sinh lỗi
    private sealed class ThrowingObserver : IAlertObserver
    {
        public void Update(AlertEvent alertEvent)
        {
            throw new InvalidOperationException("Simulated observer error");
        }
    }

    // Observer tự hủy đăng ký khi nhận cảnh báo
    private sealed class SelfRemovingObserver : IAlertObserver
    {
        private readonly SensorMonitor monitor;

        public SelfRemovingObserver(SensorMonitor monitor)
        {
            this.monitor = monitor;
        }

        public int CallCount { get; private set; }

        public void Update(AlertEvent alertEvent)
        {
            CallCount++;
            monitor.Detach(this);
        }
    }

    // ==========================================
    // 1. TEST ADAPTER PATTERN
    // ==========================================

    static void TestAdapterZero()
    {
        var legacy = new LegacySmokeDetector(0);
        ISensor sensor = new SmokeSensorAdapter(legacy);

        AssertEqual(0.0, sensor.ReadValue());
        AssertEqual("% smoke", sensor.GetUnit());
    }

    static void TestAdapterConversion()
    {
        var legacy = new LegacySmokeDetector(735);
        ISensor sensor = new SmokeSensorAdapter(legacy);

        AssertEqual(73.5, sensor.ReadValue());
    }

    static void TestAdapterMaximum()
    {
        var legacy = new LegacySmokeDetector(1000);
        ISensor sensor = new SmokeSensorAdapter(legacy);

        AssertEqual(100.0, sensor.ReadValue());
    }

    static void TestAdapterLiveReference()
    {
        var legacy = new LegacySmokeDetector(500);
        ISensor sensor = new SmokeSensorAdapter(legacy);

        AssertEqual(50.0, sensor.ReadValue());

        // Thay đổi dữ liệu của cảm biến cũ
        legacy.SetSmokeLevel(850);

        // Adapter phải đọc được dữ liệu mới
        AssertEqual(85.0, sensor.ReadValue());
    }

    static void TestAdapterNull()
    {
        AssertThrows<ArgumentNullException>(() =>
        {
            _ = new SmokeSensorAdapter(null!);
        });
    }

    // ==========================================
    // 2. TEST OBSERVER PATTERN
    // ==========================================

    static void TestNormalValue()
    {
        var monitor = new SensorMonitor();
        var observer = new RecordingObserver();

        monitor.Attach(observer);

        // 49.9%: không phát cảnh báo
        monitor.CheckSensor(
            "Smoke Sensor",
            new FakeSensor(49.9));

        AssertEqual(0, observer.CallCount);
    }

    static void TestWarningThreshold()
    {
        var monitor = new SensorMonitor();
        var observer = new RecordingObserver();

        monitor.Attach(observer);

        // Đúng ngưỡng WARNING
        monitor.CheckSensor(
            "Smoke Sensor",
            new FakeSensor(50));

        AssertEqual(1, observer.CallCount);
        AssertEqual("WARNING", observer.LastAlert!.Severity);
        AssertEqual(50.0, observer.LastAlert.Value);
    }

    static void TestCriticalThreshold()
    {
        var monitor = new SensorMonitor();
        var observer = new RecordingObserver();

        monitor.Attach(observer);

        // Đúng ngưỡng CRITICAL
        monitor.CheckSensor(
            "Smoke Sensor",
            new FakeSensor(70));

        AssertEqual(1, observer.CallCount);
        AssertEqual("CRITICAL", observer.LastAlert!.Severity);
    }

    static void TestDuplicateObserver()
    {
        var monitor = new SensorMonitor();
        var observer = new RecordingObserver();

        monitor.Attach(observer);
        monitor.Attach(observer);

        monitor.CheckSensor(
            "Smoke Sensor",
            new FakeSensor(75));

        // Observer chỉ được nhận một lần
        AssertEqual(1, observer.CallCount);
    }

    static void TestDetachObserver()
    {
        var monitor = new SensorMonitor();
        var observer = new RecordingObserver();

        monitor.Attach(observer);
        monitor.Detach(observer);

        monitor.CheckSensor(
            "Smoke Sensor",
            new FakeSensor(75));

        AssertEqual(0, observer.CallCount);
    }

    static void TestMultipleObservers()
    {
        var monitor = new SensorMonitor();

        var observer1 = new RecordingObserver();
        var observer2 = new RecordingObserver();
        var observer3 = new RecordingObserver();

        monitor.Attach(observer1);
        monitor.Attach(observer2);
        monitor.Attach(observer3);

        monitor.CheckSensor(
            "Smoke Sensor",
            new FakeSensor(80));

        AssertEqual(1, observer1.CallCount);
        AssertEqual(1, observer2.CallCount);
        AssertEqual(1, observer3.CallCount);
    }

    static void TestObserverException()
    {
        var monitor = new SensorMonitor();

        var badObserver = new ThrowingObserver();
        var goodObserver = new RecordingObserver();

        monitor.Attach(badObserver);
        monitor.Attach(goodObserver);

        // Observer đầu tiên lỗi nhưng observer thứ hai
        // vẫn phải nhận được cảnh báo.
        string output = CaptureOutput(() =>
        {
            monitor.CheckSensor(
                "Smoke Sensor",
                new FakeSensor(80));
        });

        AssertContains("[MONITOR ERROR]", output);
        AssertEqual(1, goodObserver.CallCount);
    }

    static void TestDetachDuringNotification()
    {
        var monitor = new SensorMonitor();

        var selfRemoving = new SelfRemovingObserver(monitor);
        var normalObserver = new RecordingObserver();

        monitor.Attach(selfRemoving);
        monitor.Attach(normalObserver);

        // Observer đầu tiên tự hủy đăng ký trong Update()
        monitor.CheckSensor(
            "Smoke Sensor",
            new FakeSensor(80));

        // Cả hai vẫn nhận được cảnh báo lần đầu
        AssertEqual(1, selfRemoving.CallCount);
        AssertEqual(1, normalObserver.CallCount);

        // Lần tiếp theo observer đã bị hủy đăng ký
        monitor.CheckSensor(
            "Smoke Sensor",
            new FakeSensor(80));

        AssertEqual(1, selfRemoving.CallCount);
        AssertEqual(2, normalObserver.CallCount);
    }

    static void TestNullSensor()
    {
        var monitor = new SensorMonitor();

        AssertThrows<ArgumentNullException>(() =>
        {
            monitor.CheckSensor("Smoke Sensor", null!);
        });
    }

    // ==========================================
    // 3. TEST MEDIATOR PATTERN
    // ==========================================

    static void TestFireDetected()
    {
        var alarm = new Alarm();
        var door = new DoorController();
        var ventilation = new VentilationController();

        var mediator = new EmergencyMediator(
            alarm, door, ventilation);

        var monitor = new SensorMonitor();

        var bridge = new EmergencyBridgeObserver(mediator);

        monitor.Attach(bridge);

        // Khói đạt 70% -> CRITICAL
        string output = CaptureOutput(() =>
        {
            monitor.CheckSensor(
                "Smoke Sensor",
                new FakeSensor(75));
        });

        // Kiểm tra mediator đã điều phối đủ ba thiết bị
        AssertContains("FIRE_DETECTED", output);
        AssertContains("[ALARM]", output);
        AssertContains("[DOOR]", output);
        AssertContains("[VENTILATION]", output);
    }

    static void TestWarningDoesNotActivateEmergency()
    {
        var mediator = new EmergencyMediator(
            new Alarm(),
            new DoorController(),
            new VentilationController());

        var monitor = new SensorMonitor();

        monitor.Attach(
            new EmergencyBridgeObserver(mediator));

        // WARNING không kích hoạt ứng phó khẩn cấp
        string output = CaptureOutput(() =>
        {
            monitor.CheckSensor(
                "Smoke Sensor",
                new FakeSensor(60));
        });

        AssertTrue(
            !output.Contains("FIRE_DETECTED"),
            "WARNING must not trigger emergency.");
    }

    static void TestResetSafe()
    {
        var mediator = new EmergencyMediator(
            new Alarm(),
            new DoorController(),
            new VentilationController());

        string output = CaptureOutput(() =>
        {
            mediator.Notify(
                mediator,
                "RESET_SAFE");
        });

        AssertContains("[ALARM]", output);
        AssertContains("TẮT", output);
        AssertContains("bình thường", output);
    }

    static void TestMediatorNullDependencies()
    {
        AssertThrows<ArgumentNullException>(() =>
        {
            _ = new EmergencyMediator(
                null!,
                new DoorController(),
                new VentilationController());
        });

        AssertThrows<ArgumentNullException>(() =>
        {
            _ = new EmergencyMediator(
                new Alarm(),
                null!,
                new VentilationController());
        });

        AssertThrows<ArgumentNullException>(() =>
        {
            _ = new EmergencyMediator(
                new Alarm(),
                new DoorController(),
                null!);
        });
    }
}
