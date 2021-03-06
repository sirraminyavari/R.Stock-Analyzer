use Stock
go

DROP TABLE #Result
GO

declare @Count int = 60
declare @Symbols TABLE(Symbol nvarchar(100))

INSERT INTO @Symbols (Symbol)
VALUES (N'زاگرس'), (N'فاسمین'), (N'فملی'), (N'فیروزه'), (N'کاردان'), (N'اطلس'), (N'افق_ملت'), (N'آگاس'), (N'کاریس'), (N'کروی'), (N'مبین'), (N'واحیا'), (N'وخاور')

SELECT	X.Symbol,
		X.Number AS [Order],
		X.Rate
INTO #Result
FROM (
		SELECT	ROW_NUMBER() OVER(PARTITION BY S.Symbol ORDER BY S.[Date] DESC) AS Number,
				S.Symbol, 
				S.[Date],
				ROUND(((S.ClosingPrice / S.PreviousPrice) - 1) * 100, 2) AS Rate
		FROM @Symbols AS R 
			INNER JOIN [dbo].[ST_StockHistory] AS S
			ON S.Symbol = R.Symbol
	) AS X
WHERE X.Number <= @Count
ORDER BY X.Symbol DESC, X.Number ASC

DECLARE @lst nvarchar(max)

SELECT @lst = COALESCE(@lst + ', ', '') + N'[زمان ' + CAST(Q.[Order] AS varchar(max)) + ']'
FROM (
		SELECT DISTINCT Ref.[Order]
		FROM #Result AS Ref
	) AS Q
ORDER BY Q.[Order] ASC

DECLARE @Proc nvarchar(max) = 
	'SELECT * ' +
	'FROM ( ' +
			N'SELECT symbol AS [نماد], ' + @lst + ' ' +
			'FROM ( ' +
					N'SELECT Symbol, ''زمان '' + CAST([Order] AS varchar(10)) AS [Order], Rate ' +
					'FROM #Result ' +
				') P ' +
				'PIVOT (MAX(Rate) FOR [Order] IN (' + @lst + ')) AS pvt ' +
		') AS TableName'

EXEC (@Proc)