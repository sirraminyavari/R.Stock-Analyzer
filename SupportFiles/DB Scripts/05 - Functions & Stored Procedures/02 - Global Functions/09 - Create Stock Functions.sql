USE [Stock]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ST_FN_GetLastActiveDate]') 
            AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
DROP FUNCTION [dbo].[ST_FN_GetLastActiveDate]
GO

CREATE FUNCTION [dbo].[ST_FN_GetLastActiveDate](
	@IgnoreDays	int
)	
RETURNS DATETIME
WITH ENCRYPTION
AS
BEGIN
	RETURN (
		SELECT TOP(1) X.[Date]
		FROM (
				SELECT	ROW_NUMBER() OVER(ORDER BY H.[Date] DESC) AS RowNumber,
						H.[Date]
				FROM [dbo].[ST_StockHistory] AS H
				GROUP BY H.[Date]
			) AS X
		WHERE X.RowNumber > ISNULL(@IgnoreDays, 0)
		ORDER BY X.RowNumber ASC
	)
END

GO
