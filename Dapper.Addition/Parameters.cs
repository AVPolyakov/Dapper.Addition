using System.Data;
using Dapper;

namespace Dapper.Addition
{
    public class Parameters : SqlMapper.IDynamicParameters
    {
        private readonly DynamicParameters _dynamicParameters = new();
        
        public void AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            SqlMapper.IDynamicParameters parameters = _dynamicParameters;
            parameters.AddParameters(command, identity);
            
            CheckMapping(command, identity);
        }
        
        /// <summary>
        /// Adds parameters in format <code>new {A = 1, B = 2}</code> 
        /// </summary>
        public void AddParameters(object param) => _dynamicParameters.AddDynamicParams(param);
        
        private static void CheckMapping(IDbCommand command, SqlMapper.Identity identity)
        {
            if (!Sql.MappingCheckEnabled)
                return;

            if (identity.type == null)
                return;

            var connection = command.Connection!;

            var wasClosed = connection.State == ConnectionState.Closed;
            try
            {
                if (wasClosed)
                    connection.Open();

                using (var reader = command.ExecuteReader(CommandBehavior.SchemaOnly))
                    reader.CheckMapping(identity.type);
            }
            finally
            {
                if (wasClosed)
                    connection.Close();
            }
        }
    }
}