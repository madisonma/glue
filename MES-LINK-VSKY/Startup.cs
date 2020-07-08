using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MES_LINK_VSKY.models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog.Extensions.Logging;
using NLog.Web;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.OpenApi.Models;
namespace MES_LINK_VSKY
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            new Appsetting().Initial(configuration);//配置文件注入
        }
        public IConfiguration Configuration { get; }
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options => options.EnableEndpointRouting = false).SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            services.Configure<ConnectionStrings>(Configuration.GetSection("ConnectionStrings")).AddMvc();
            services.AddSwaggerGen(c =>
            {
                //添加Swagger.
                
                c.SwaggerDoc("VSKY", new OpenApiInfo//Controller 分组名
                {
                    Version = "1.0.0",
                    Title = "WEB API接口文档-VSKY",
                    Description = "用于BU22MES Link VSKY(for NXT),控制by工单上料/PCB",
                    //服务条款
                    ////TermsOfService = "None",
                    //作者信息
                    Contact = new OpenApiContact
                    {
                        Name = "Spark",
                        Email = "Spark.Huang@luxshare-ict.com",
                        Url = new Uri("http://10.33.10.61:9000"),
                    },
                    //许可证
                    //License = new License
                    //{
                    //    Name = "许可证名字",
                    //    Url = "http://www.cnblogs.com/Scholars/"
                    //}
                });
                c.SwaggerDoc("ASM", new OpenApiInfo
                {
                    Version = "1.0.0",
                    Title = "WEB API接口文档-ASM",
                    Description = "用于BU22MES Link ASM Machines,防错及追溯",
                    //服务条款
                    ////TermsOfService = "None",
                    //作者信息
                    Contact = new OpenApiContact
                    {
                        Name = "Spark",
                        Email = "Spark.Huang@luxshare-ict.com",
                        Url = new Uri("http://10.33.10.61:9000"),
                    },
                    //许可证
                    //License = new License
                    //{
                    //    Name = "许可证名字",
                    //    Url = "http://www.cnblogs.com/Scholars/"
                    //}
                });
                c.SwaggerDoc("RAS", new OpenApiInfo
                {
                    Version = "1.0.0",
                    Title = "WEB API接口文档-RAS",
                    Description = "用于Rasberry Link MES",
                    //服务条款
                    ////TermsOfService = "None",
                    //作者信息
                    Contact = new OpenApiContact
                    {
                        Name = "Spark",
                        Email = "Spark.Huang@luxshare-ict.com",
                        Url = new Uri("http://10.33.10.61:9000"),
                    },
                    //许可证
                    //License = new License
                    //{
                    //    Name = "许可证名字",
                    //    Url = "http://www.cnblogs.com/Scholars/"
                    //}
                });
                c.SwaggerDoc("ROBOT", new OpenApiInfo
                {
                    Version = "1.0.0",
                    Title = "WEB API接口文档-自动化",
                    Description = "用于自动化 Link MES",
                    //服务条款
                    ////TermsOfService = "None",
                    //作者信息
                    Contact = new OpenApiContact
                    {
                        Name = "Spark",
                        Email = "Spark.Huang@luxshare-ict.com",
                        Url = new Uri("http://10.33.10.61:9000"),
                    },
                    //许可证
                    //License = new License
                    //{
                    //    Name = "许可证名字",
                    //    Url = "http://www.cnblogs.com/Scholars/"
                    //}
                });
                c.SwaggerDoc("Glue", new OpenApiInfo
                {
                    Version = "1.0.0",
                    Title = "WEB API接口文档-点胶机",
                    Description = "用于点胶机 Link MES",
                    //服务条款
                    ////TermsOfService = "None",
                    //作者信息
                    Contact = new OpenApiContact
                    {
                        Name = "Spark",
                        Email = "Spark.Huang@luxshare-ict.com",
                        Url = new Uri("http://10.33.10.61:9000"),
                    },
                    //许可证
                    //License = new License
                    //{
                    //    Name = "许可证名字",
                    //    Url = "http://www.cnblogs.com/Scholars/"
                    //}
                });
                // 下面三个方法为 Swagger JSON and UI设置xml文档注释路径
                var basePath = Path.GetDirectoryName(typeof(Program).Assembly.Location);//获取应用程序所在目录（绝对，不受工作目录影响，建议采用此方法获取路径）
                var xmlPath = Path.Combine(basePath, "MES_LINK_VSKY.xml");
                c.IncludeXmlComments(xmlPath);
            });
            //services.AddCors();
            services.AddCors(option => option.AddPolicy("cors", builder => builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader()));//.WithOrigins("http://10.35.209.247")
            //启用ajax跨域调用
            //读取aoosettings.json里配置的数据库连接语句需要的代码
            //services.Configure<DBContext>(Configuration.GetSection("SqlConfiguration"));

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseOptions();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            
            loggerFactory.AddNLog();//添加NLog                     //引入Nlog配置文件
            env.ConfigureNLog("nlog.config");
            app.UseSwagger();
            //Swagger Core需要配置的  必须加在app.UseMvc前面
            app.UseSwaggerUI(c =>
            {

                c.SwaggerEndpoint("/swagger/VSKY/swagger.json", "VSKY");
                c.SwaggerEndpoint("/swagger/ASM/swagger.json", "ASM"); //ASM Controller 分组名
                c.SwaggerEndpoint("/swagger/RAS/swagger.json", "RAS");
                c.SwaggerEndpoint("/swagger/ROBOT/swagger.json", "ROBOT");
                c.SwaggerEndpoint("/swagger/Glue/swagger.json", "Glue");
            });
            //app.UseHttpsRedirection();
            app.UseCors("cors");//启用ajax跨域全局调用
            //        app.UseCors(options => options
            //    .WithOrigins("http://localhost:5001")
            //    .AllowAnyHeader()
            //    .AllowAnyMethod()
            //    .AllowCredentials()
            //    .AllowAnyOrigin()
            //);
            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            app.UseMvcWithDefaultRoute();
            //app.UseMvc();
        }
    }
}
