SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
IF DB_ID(N'KaraokeDB') IS NULL EXEC(N'CREATE DATABASE [KaraokeDB]');
GO
USE [KaraokeDB];
GO
IF OBJECT_ID(N'dbo.PhongHat',N'U') IS NULL
BEGIN
SET XACT_ABORT ON;
BEGIN TRANSACTION;

CREATE TABLE PhongHat(MaPhong int IDENTITY PRIMARY KEY,TenPhong nvarchar(50) NOT NULL,LoaiPhong nvarchar(30),GiaTheoGio decimal(18,2) NOT NULL CHECK(GiaTheoGio>=0),TrangThai nvarchar(20) NOT NULL DEFAULT N'Trống' CHECK(TrangThai IN(N'Trống',N'Đang hát')));
CREATE TABLE DichVu(MaDV int IDENTITY PRIMARY KEY,TenDV nvarchar(100) NOT NULL,DonGia decimal(18,2) NOT NULL CHECK(DonGia>=0),TonKho int NOT NULL CHECK(TonKho>=0));
CREATE TABLE HoaDon(MaHD int IDENTITY PRIMARY KEY,MaPhong int NOT NULL REFERENCES PhongHat,GioVao datetime NOT NULL,GioRa datetime NULL,TongTienPhong decimal(18,2) NOT NULL DEFAULT 0,TongTienDichVu decimal(18,2) NOT NULL DEFAULT 0,TrangThai bit NOT NULL DEFAULT 0);
CREATE UNIQUE INDEX UX_HoaDon_PhongDangHat ON HoaDon(MaPhong) WHERE TrangThai=0;
CREATE TABLE ChiTietDichVu(MaHD int REFERENCES HoaDon,MaDV int REFERENCES DichVu,SoLuong int NOT NULL CHECK(SoLuong>0),DonGia decimal(18,2) NOT NULL,PRIMARY KEY(MaHD,MaDV));
INSERT PhongHat(TenPhong,LoaiPhong,GiaTheoGio) VALUES(N'Phòng VIP 01',N'VIP',180000),(N'Phòng Thường 02',N'Thường',100000),(N'Phòng Thường 03',N'Thường',100000);
INSERT DichVu(TenDV,DonGia,TonKho) VALUES(N'Bia Heineken',30000,20),(N'Trái cây dĩa',75000,5),(N'Nước suối',10000,30);

COMMIT;
END;
GO
