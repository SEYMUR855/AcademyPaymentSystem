using AcademyPayment.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using AcademyPayment.Exceptions;
using AcademyPayment.Extensions;
using AcademyPayment.DAL.DbSets;
using System.Reflection;

namespace AcademyPayment.DAL
{
    public abstract class _DbContext : IDbContext
    {


        private DbConnection dbConnection;
        private DbProviderFactory providerFactory;

        //private DbConfiguration dbConfiguration;

        public _DbContext(string connectionName)
        {
            if (String.IsNullOrEmpty(connectionName))
                throw new ArgumentException("Argument shouldn't be null!!!", new ArgumentNullException());

            Connect(connectionName);
        }

        /// <summary>
        /// Initialize DbSets
        /// </summary>
        /// 
        public void Initialize(_DbContext context)
        {
            InitializeDbSets(context);
        }
        private void InitializeDbSets(_DbContext derivedClass)
        {
            IEnumerable<PropertyInfo> properties = derivedClass.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo item in properties)
            {
                if (item.PropertyType.IsGenericType)
                {
                    Type GenericDefinition = item.PropertyType.GetGenericTypeDefinition();
                    if (GenericDefinition == typeof(_DbSet<>))
                    {
                        #region Create Generic Type
                        Type[] types = item.PropertyType.GetGenericArguments();
                        Type constructed = GenericDefinition.MakeGenericType(types);
                        object obj = Activator.CreateInstance(constructed);
                        #endregion
                        SendProviderAndConnection(obj,constructed);
                        item.SetValue(derivedClass, obj);
                    }
                }
            }
        }
        private void SendProviderAndConnection(Object obj , Type type)
        {
           
                IEnumerable<FieldInfo> dbSetFields = type.GetFields();
                foreach (FieldInfo elem in dbSetFields)
                {
                    if (elem.FieldType == dbConnection.GetType().BaseType)
                    {
                        elem.SetValue(obj,dbConnection);
                    }
                    else if (elem.FieldType == providerFactory.GetType().BaseType)
                    {
                        elem.SetValue(obj,providerFactory);
                    }
                }
            
        }
        /// <summary>
        /// connect to database 
        /// </summary>
        /// <param name="connectionName"></param>
        /// <param name="providerKey"></param>
        private void Connect(string connectionName)
        {
            //Get provider factory

            providerFactory = GetDbFactory(connectionName);

            //Read connection from configuration file
            string connection = ConfigurationOperation.GetConnectionString(connectionName);

            if (String.IsNullOrEmpty(connection))
                throw new InvalidConnectionException("This connection name is not valid !!!");

            try
            {
                //Create DbConnection instance and open it
                dbConnection = providerFactory.CreateConnection();
                dbConnection.ConnectionString = connection;
                dbConnection.Open();
            }
            catch (DbException ex)
            {
                throw new InvalidConnectionException(ex.Message,ex);
            }
        }

        private DbProviderFactory GetDbFactory(string connectionName)
        {
            try
            {
                //Get provider factory from factories
                return DbProviderFactories.GetFactory(ConfigurationOperation.GetProviderName(connectionName));
            }
            catch (ArgumentException ex)
            {
                throw new InvalidProviderException(ex.Message, ex);
            }
        }

        //Dispose connection
        public virtual void Dispose()
        {
            dbConnection.Close();
        }
    }
}
