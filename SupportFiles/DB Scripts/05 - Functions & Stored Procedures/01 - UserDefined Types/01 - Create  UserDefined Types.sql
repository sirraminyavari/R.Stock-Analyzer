/****** Object:  StoredProcedure [dbo].[AddFolder]    Script Date: 03/14/2012 11:38:59 ******/
USE [Stock]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


IF NOT EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'StockItemTableType')
--DROP TYPE dbo.StockItemTableType

CREATE TYPE [dbo].[StockItemTableType] AS TABLE(
	[Code] varchar(10) NOT NULL,
	[Date] datetime NOT NULL,
	[Symbol] nvarchar(10) NOT NULL,
	[PreviousPrice] float NOT NULL,
	[OpeningPrice] float NOT NULL,
	[ClosingPrice] float NOT NULL,
	[HighestPrice] float NOT NULL,
	[LowestPrice] float NOT NULL,
	[LastPrice] float NOT NULL,
	[Volume] float NOT NULL,
	[TotalValue] float NOT NULL,
	[TotalTransactions] float NOT NULL,
	[Name] nvarchar(100) NOT NULL,
	[LatinName] nvarchar(100) NOT NULL
)
GO


IF NOT EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'DateTableType')
--DROP TYPE dbo.DateTableType

CREATE TYPE [dbo].[DateTableType] AS TABLE(
	[Value] datetime NOT NULL primary key clustered
)
GO

IF NOT EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'StringTableType')
--DROP TYPE dbo.StringTableType

CREATE TYPE [dbo].[StringTableType] AS TABLE(
	[Value] nvarchar(1000) NOT NULL primary key clustered
)
GO