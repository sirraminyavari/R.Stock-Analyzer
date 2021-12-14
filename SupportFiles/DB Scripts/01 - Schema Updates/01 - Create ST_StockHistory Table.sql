USE [Stock]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[ST_StockHistory](
	Symbol nvarchar(100) NOT NULL,
	[Date] datetime NOT NULL,
	Code nvarchar(50) NULL,
	PreviousPrice float NOT NULL,
	OpeningPrice float NOT NULL,
	ClosingPrice float NOT NULL,
	HighestPrice float NOT NULL,
	LowestPrice float NOT NULL,
	LastPrice float NOT NULL,
	Volume float NOT NULL,
	TotalValue float NOT NULL,
	TotalTransactions float NOT NULL,
	Name nvarchar(200) NULL,
	LatinName nvarchar(200) NULL
 CONSTRAINT [PK_ST_StockHistory] PRIMARY KEY CLUSTERED 
(
	[Symbol] ASC,
	[Date] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO
