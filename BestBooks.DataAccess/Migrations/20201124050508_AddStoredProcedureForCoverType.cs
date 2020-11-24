using Microsoft.EntityFrameworkCore.Migrations;

namespace BestBooks.DataAccess.Migrations
{
    public partial class AddStoredProcedureForCoverType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE PROCEDURE usp_GetCoverTypes()
                                    BEGIN
	                                    SELECT * FROM   dbo.CoverTypes;
                                    END");

            migrationBuilder.Sql(@"CREATE PROCEDURE usp_GetCoverType(
                                     IN Id int 
                                    ) 
                                    BEGIN 
                                       SELECT * FROM   dbo.CoverTypes  WHERE  (Id = Id); 
                                    END ");

            migrationBuilder.Sql(@"CREATE PROCEDURE usp_UpdateCoverType(
                                    IN Id int,
                                    IN Name varchar(100)
                                    )
                                    BEGIN 
                                     UPDATE dbo.CoverTypes
                                     SET  Name = Name
                                     WHERE  Id = Id;
                                    END");

            migrationBuilder.Sql(@"CREATE PROCEDURE usp_DeleteCoverType(
                                    IN Id int
                                    )
                                    BEGIN 
                                     DELETE FROM dbo.CoverTypes
                                     WHERE  Id = Id;
                                    END");

            migrationBuilder.Sql(@"CREATE PROCEDURE usp_CreateCoverType(
                                    IN Name varchar(100)
                                    )
                                    BEGIN 
                                    INSERT INTO dbo.CoverTypes(Name)
                                    VALUES (Name);
                                    END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP PROCEDURE usp_GetCoverTypes");
            migrationBuilder.Sql(@"DROP PROCEDURE usp_GetCoverType");
            migrationBuilder.Sql(@"DROP PROCEDURE usp_UpdateCoverType");
            migrationBuilder.Sql(@"DROP PROCEDURE usp_DeleteCoverType");
            migrationBuilder.Sql(@"DROP PROCEDURE usp_CreateCoverType");
        }
    }
}
