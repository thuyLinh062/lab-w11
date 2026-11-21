//Họ và Tên: Trần Thùy Linh
//MSSV: 202418935
using System;
using System.Collections.Generic;

namespace CampusSafetyHub;

public interface ISensor
{
    double ReadValue();
    string GetUnit();
}
