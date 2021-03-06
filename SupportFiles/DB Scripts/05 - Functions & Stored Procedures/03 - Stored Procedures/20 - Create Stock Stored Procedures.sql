USE [Stock]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[ST_AddStockHistory]') AND
	OBJECTPROPERTY(id, N'IsProcedure') = 1)
DROP PROCEDURE [dbo].[ST_AddStockHistory]
GO

CREATE PROCEDURE [dbo].[ST_AddStockHistory]
	@StockTemp	StockItemTableType readonly
WITH ENCRYPTION
AS
BEGIN
	SET NOCOUNT ON
	
	DECLARE @Stock StockItemTableType
	INSERT INTO @Stock SELECT * FROM @StockTemp
	
	-- Prepare Data
	UPDATE @Stock
		SET Symbol = [dbo].[GFN_VerifyString](Symbol),
			Name = [dbo].[GFN_VerifyString](Name)
	-- end of Prepare Data
	
	UPDATE H
		SET Symbol = ST.Symbol,
			PreviousPrice = ST.PreviousPrice,
			OpeningPrice = ST.OpeningPrice,
			ClosingPrice = ST.ClosingPrice,
			HighestPrice = ST.HighestPrice,
			LowestPrice = ST.LowestPrice,
			LastPrice = ST.LastPrice,
			Volume = ST.Volume,
			TotalValue = ST.TotalValue,
			TotalTransactions = ST.TotalTransactions,
			Name = ST.Name,
			LatinName = ST.LatinName
	FROM @Stock AS ST
		INNER JOIN [dbo].[ST_StockHistory] AS H
		ON H.Symbol = ST.Symbol AND H.[Date] = ST.[Date]
		
	DECLARE @Result int = @@ROWCOUNT	
		
	INSERT INTO [dbo].[ST_StockHistory] (Symbol, [Date], Code, PreviousPrice, OpeningPrice, ClosingPrice, 
		HighestPrice, LowestPrice, LastPrice, Volume, TotalValue, TotalTransactions, Name, LatinName)
	SELECT	ST.Symbol,
			ST.[Date],
			ST.Code,
			ST.PreviousPrice,
			ST.OpeningPrice,
			ST.ClosingPrice,
			ST.HighestPrice,
			ST.LowestPrice,
			ST.LastPrice,
			ST.Volume,
			ST.TotalValue,
			ST.TotalTransactions,
			ST.Name,
			ST.LatinName
	FROM @Stock AS ST
		LEFT JOIN [dbo].[ST_StockHistory] AS H
		ON H.Symbol = ST.Symbol AND H.[Date] = ST.[Date]
	WHERE H.Code IS NULL
	
	SELECT @@ROWCOUNT + @Result
END

GO


IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[ST_CollectDataForAnalysis]') AND
	OBJECTPROPERTY(id, N'IsProcedure') = 1)
DROP PROCEDURE [dbo].[ST_CollectDataForAnalysis]
GO

CREATE PROCEDURE [dbo].[ST_CollectDataForAnalysis]
	@LastNDates int,
	@IgnoreFirstNDays int
WITH ENCRYPTION
AS
BEGIN
	SET NOCOUNT ON
	
	DECLARE @Dates DateTableType

	INSERT INTO @Dates(Value)
	SELECT TOP(ISNULL(@LastNDates, 30)) X.[Date]
	FROM (
			SELECT	ROW_NUMBER() OVER(ORDER BY H.[Date] DESC) AS RowNumber,
					H.[Date]
			FROM [dbo].[ST_StockHistory] AS H
			GROUP BY H.[Date]
		) AS X
	WHERE X.RowNumber > ISNULL(@IgnoreFirstNDays, 0)
	ORDER BY X.RowNumber ASC

	DECLARE @MaxDate datetime = (SELECT TOP(1) MAX(D.Value) FROM @Dates AS D)

	SELECT	H.Code,
			H.Symbol,
			H.[Date],
			DATEDIFF(DAY, H.[Date], @MaxDate) AS [DateDiff],
			DATEPART(WEEKDAY, H.[Date]) AS [DayOfWeek],
			CASE
				WHEN H.PreviousPrice = 1000 THEN 0
				ELSE ROUND((H.ClosingPrice / H.PreviousPrice) - 1, 6)
			END AS ChangeRate,
			ROUND((H.HighestPrice / H.LowestPrice) - 1, 6) AS HighestByLowest
	FROM @Dates AS D
		INNER JOIN [dbo].[ST_StockHistory] AS H
		ON H.[Date] = D.Value
	WHERE H.Symbol NOT LIKE N'%ح' -- Ignore حق تقدم
	ORDER BY H.[Date] DESC
END

GO


IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[ST_GetLastActiveDate]') AND
	OBJECTPROPERTY(id, N'IsProcedure') = 1)
DROP PROCEDURE [dbo].[ST_GetLastActiveDate]
GO

CREATE PROCEDURE [dbo].[ST_GetLastActiveDate]
	@IgnoreDays		int
WITH ENCRYPTION
AS
BEGIN
	SET NOCOUNT ON
	
	SELECT [dbo].[ST_FN_GetLastActiveDate](@IgnoreDays)
END

GO


IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[ST_GetActiveSymbols]') AND
	OBJECTPROPERTY(id, N'IsProcedure') = 1)
DROP PROCEDURE [dbo].[ST_GetActiveSymbols]
GO

CREATE PROCEDURE [dbo].[ST_GetActiveSymbols]
	@SearchText		nvarchar(50),
	@IgnoreDays		int,
	@DateFrom		datetime,
	@DateTo			datetime,
	@Count			int,
	@LowerBoundary	int
WITH ENCRYPTION
AS
BEGIN
	SET NOCOUNT ON
	
	IF @SearchText IS NOT NULL SET @SearchText = [dbo].[GFN_VerifyString](@SearchText)
	
	IF(@DateTo IS NULL AND @IgnoreDays > 0)
		SET @DateTo = [dbo].[ST_FN_GetLastActiveDate](@IgnoreDays)
	
	SELECT TOP(ISNULL(@Count, 1000000)) 
		X.Symbol, 
		CAST(X.RowNumber + X.RevRowNumber - 1 AS bigint) AS TotalCount
	FROM (
			SELECT	ROW_NUMBER() OVER (ORDER BY ST.Symbol ASC) AS RowNumber,
					ROW_NUMBER() OVER (ORDER BY ST.Symbol DESC) AS RevRowNumber,
					ST.Symbol
			FROM [dbo].[ST_StockHistory] AS ST
			WHERE ST.Symbol NOT LIKE N'%ح' AND  -- Ignore حق تقدم
				(@DateFrom IS NULL OR ST.[Date] >= @DateFrom) AND
				(@DateTO IS NULL OR ST.[Date] <= @DateTO) AND 
				(
					ISNULL(@SearchText, N'') = N'' OR
					(ST.Symbol + N' ' + ISNULL(ST.Code, N'') + N' ' + ISNULL(ST.Name, N'') + 
						N' ' + ISNULL(ST.LatinName, N'')) LIKE (N'%' + @SearchText + N'%')
				) AND
				((ST.HighestPrice <> ST.OpeningPrice OR ST.HighestPrice <> ST.ClosingPrice)) AND
				ST.TotalTransactions > 100 AND
				ST.Symbol NOT LIKE N'%[0-9]%' AND
				ST.Symbol NOT LIKE N'%شاخ%'
			GROUP BY ST.Symbol
		) AS X
	WHERE X.RowNumber >= ISNULL(@LowerBoundary, 0)
	ORDER BY X.RowNumber ASC
END

GO


IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[ST_GetMarketROC]') AND
	OBJECTPROPERTY(id, N'IsProcedure') = 1)
DROP PROCEDURE [dbo].[ST_GetMarketROC]
GO

CREATE PROCEDURE [dbo].[ST_GetMarketROC]
	@IgnoreDays		int,
	@DateFrom		datetime,
	@DateTo			datetime
WITH ENCRYPTION
AS
BEGIN
	SET NOCOUNT ON
	
	IF(@DateTo IS NULL AND @IgnoreDays > 0)
		SET @DateTo = [dbo].[ST_FN_GetLastActiveDate](@IgnoreDays)
	
	SELECT	H.[Date], 
			ROUND(SUM(((H.ClosingPrice / H.PreviousPrice) - 1) * H.TotalValue) / 
				SUM(H.TotalValue), 6) * 100 AS Value
	FROM [dbo].[ST_StockHistory] AS H
	WHERE H.PreviousPrice > 0 AND H.ClosingPrice > 0 AND H.TotalValue > 0 AND
		(@DateFrom IS NULL OR H.[Date] >= @DateFrom) AND
		(@DateTO IS NULL OR H.[Date] <= @DateTO) AND 
		H.Symbol NOT LIKE N'%ح' AND  -- Ignore حق تقدم
		((H.HighestPrice <> H.OpeningPrice OR H.HighestPrice <> H.ClosingPrice)) AND
		H.TotalTransactions > 100 AND
		H.Symbol NOT LIKE N'%[0-9]%'
	GROUP BY H.[Date]
	ORDER BY H.[Date] DESC
END

GO


IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[ST_GetSymbolData]') AND
	OBJECTPROPERTY(id, N'IsProcedure') = 1)
DROP PROCEDURE [dbo].[ST_GetSymbolData]
GO

CREATE PROCEDURE [dbo].[ST_GetSymbolData]
	@strSymbols		nvarchar(max),
	@delimiter		char,
	@IgnoreDays		int,
	@DateFrom		datetime,
	@DateTo			datetime
WITH ENCRYPTION
AS
BEGIN
	SET NOCOUNT ON
	
	DECLARE @Symbols StringTableType
	
	INSERT INTO @Symbols (Value)
	SELECT DISTINCT [dbo].[GFN_VerifyString](Ref.Value)
	FROM [dbo].[GFN_StrToStringTable](@strSymbols, @delimiter) AS Ref
	
	IF(@DateTo IS NULL AND @IgnoreDays > 0)
		SET @DateTo = [dbo].[ST_FN_GetLastActiveDate](@IgnoreDays)
	
	SELECT	H.Symbol,
			H.[Date],
			H.Code,
			H.PreviousPrice,
			H.OpeningPrice,
			H.ClosingPrice,
			H.HighestPrice,
			H.LowestPrice,
			H.LastPrice,
			H.Volume,
			H.TotalValue,
			H.TotalTransactions
	FROM @Symbols AS S
		INNER JOIN [dbo].[ST_StockHistory] AS H
		ON H.Symbol = S.Value
	WHERE (@DateFrom IS NULL OR H.[Date] >= @DateFrom) AND
		(@DateTo IS NULL OR H.[Date] <= @DateTo) AND H.Volume > 0 AND H.ClosingPrice > 0
	ORDER BY H.[Date] ASC
END

GO


IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[ST_GetMarketSymbolsData]') AND
	OBJECTPROPERTY(id, N'IsProcedure') = 1)
DROP PROCEDURE [dbo].[ST_GetMarketSymbolsData]
GO

CREATE PROCEDURE [dbo].[ST_GetMarketSymbolsData]
	@IgnoreDays		int,
	@DateFrom		datetime,
	@DateTo			datetime
WITH ENCRYPTION
AS
BEGIN
	SET NOCOUNT ON
	
	DECLARE @strSymbols nvarchar(1000) = N'شاخص_صنعت6' + N',' + N'شاخص_قیمت6' + N',' + N'شاخص_کل6'
	
	EXEC [dbo].[ST_GetSymbolData] @strSymbols, N',', @IgnoreDays, @DateFrom, @DateTo
END

GO