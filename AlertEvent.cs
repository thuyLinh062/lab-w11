//Họ và Tên: Trần Thùy Linh
//MSSV: 202418935
using System;
using System.Collections.Generic;

namespace CampusSafetyHub;

public sealed record AlertEvent(
    string SensorName,
    double Value,
    string Unit,
    string Severity,
    DateTime OccurredAt);