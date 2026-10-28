-- SQL Server snippets paired with the EF Core Product / Category sample.
-- Run only after creating the matching tables in a disposable learning database.

-- SQL Q1, Q2, Q15, Q40, Q51: joins, filtering, grouping, HAVING, and ON conditions.
SELECT c.Name AS Category, COUNT(*) AS ProductCount, AVG(p.Price) AS AveragePrice
FROM dbo.Categories AS c
LEFT JOIN dbo.Products AS p ON p.CategoryId = c.Id
GROUP BY c.Name
HAVING COUNT(p.Id) >= 1;

-- SQL Q13, Q14: stable offset pagination. Add a matching index for larger data sets.
DECLARE @Page int = 1, @PageSize int = 10;
SELECT Id, Name, Price
FROM dbo.Products
ORDER BY Id
OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;

-- SQL Q11, Q12, Q41, Q71: window functions, CTEs, ranking, and frames/partitions.
WITH RankedProducts AS
(
    SELECT p.Id, p.Name, p.Price, c.Name AS Category,
           ROW_NUMBER() OVER (PARTITION BY p.CategoryId ORDER BY p.Price DESC, p.Id) AS PriceRank
    FROM dbo.Products AS p
    JOIN dbo.Categories AS c ON c.Id = p.CategoryId
)
SELECT Id, Name, Price, Category, PriceRank
FROM RankedProducts
WHERE PriceRank <= 3;

-- SQL Q9, Q48, Q62: transaction boundaries, concurrency check, and OUTPUT values.
BEGIN TRANSACTION;
UPDATE dbo.Products
SET Stock = Stock - @Quantity
OUTPUT inserted.Id, deleted.Stock AS OldStock, inserted.Stock AS NewStock
WHERE Id = @ProductId AND Stock >= @Quantity;
-- Application should verify a row was updated, then COMMIT; otherwise ROLLBACK.
ROLLBACK TRANSACTION; -- Safe demonstration default; replace only in a disposable DB.

-- SQL Q24, Q37; EF Core Q195: parameterized search prevents SQL injection.
SELECT Id, Name, Price
FROM dbo.Products
WHERE Name LIKE N'%' + @SearchTerm + N'%';

-- Example supporting index (review write cost and selectivity before using in a real schema).
CREATE INDEX IX_Products_CategoryId_Id ON dbo.Products(CategoryId, Id) INCLUDE (Name, Price, Stock);
