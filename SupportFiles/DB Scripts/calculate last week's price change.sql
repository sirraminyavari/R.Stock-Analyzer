USE [Stock]
GO



DECLARE @FromNDaysAgo int = 7
DECLARE @PeriodLength int = 5

DECLARE @MinDate datetime = DATEADD(DAY, -ISNULL(@FromNDaysAgo, 0), GETDATE())

SELECT	A.Symbol,
		B.FirstPrice,
		A.LastPrice,
		A.MaxPrice,
		A.MinPrice,
		ROUND(((A.LastPrice / B.FirstPrice) - 1) * 100, 2) AS RateOfChange,
		ROUND(((A.MaxPrice / B.FirstPrice) - 1) * 100, 2) AS MaxGrowth,
		ROUND(((A.MinPrice / B.FirstPrice) - 1) * 100, 2) AS MinGrowth,
		A.ActiveDays
FROM (
		SELECT	X.Symbol, 
				MAX(X.ClosingPrice) AS MaxPrice,
				MIN(X.ClosingPrice) AS MinPrice,
				SUM(CASE WHEN X.Number = 1 THEN X.ClosingPrice ELSE 0 END) AS LastPrice,
				MAX(X.Number) AS ActiveDays
		FROM (
				SELECT	ROW_NUMBER() OVER(PARTITION BY H.Symbol ORDER BY H.[Date] DESC) AS Number, 
						H.*
				FROM [dbo].[ST_StockHistory] AS H
				WHERE H.[Date] >= @MinDate
			) AS X
		WHERE X.Number <= @PeriodLength
		GROUP BY X.Symbol
	) AS A
	INNER JOIN (
		SELECT S.Symbol, S.ClosingPrice AS FirstPrice
		FROM [dbo].[ST_StockHistory] AS S
			INNER JOIN (
				SELECT H.Symbol, MAX(H.[Date]) AS MaxDate
				FROM [dbo].[ST_StockHistory] AS H
				WHERE H.[Date] < @MinDate
				GROUP BY H.Symbol
			) AS Y
			ON Y.Symbol = S.Symbol AND Y.[MaxDate] = S.[Date]
	) AS B
	ON B.Symbol = A.Symbol