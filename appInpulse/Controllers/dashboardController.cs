namespace Controllers
{
    using Infra.Base.Interface.Base;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Entity;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Web.Http;
    using System.Web.Http.Description;
    using Infra.Base;
    using System.IO;
    using System.Drawing;
    using System.Web;

    public class dashboardController : CrudControllerBase<v_operadores_status>
    {
        private Context dblocal = new Context();
        private int DISCADAS = 0;
        private int CONTATOS = 0;
        private int PEDIDOS = 0;
        private double PRODUTIVIDADE = 0;
        private double APROVEITAMENTO = 0;
        private dynamic VendasPorEstado;
        private dynamic MetasXVendas;

        //public dashboardController()
        //{
        //    ControllerBaseIP IP = new ControllerBaseIP();
        //    var x = IP.GetClientIp(this.Request);
        //    x = "";
        //}

        protected override IOrderedQueryable<v_operadores_status> Ordenar(IQueryable<v_operadores_status> query)
        {
            return query.OrderBy(e => e.NOME);
        }

        protected override IQueryable<v_operadores_status> TrazerDadosParaLista(IQueryable<v_operadores_status> query)
        {
            int i = 0;
            foreach (var item in query)
            {
                i++;
                var foto = dblocal.Set<operadores_foto>().Where(q => q.id == item.id).FirstOrDefault();

                if (foto != null)
                {
                    var diretorio = HttpContext.Current.Server.MapPath("~") + @"\img\" + item.id.ToString() + ".png";
                    item.FOTO = item.id.ToString() + ".png";
                    var f = byteArrayToImage(foto.FOTO);
                    f.Save(diretorio);
                }

                GetValores(item.id, null, null);
                item.APROVEITAMENTO = APROVEITAMENTO;
                item.CONTATOS = CONTATOS;
                item.LIGACOES = DISCADAS;
                item.PRODUTIVIDADE = PRODUTIVIDADE;
                item.PEDIDOS = PEDIDOS;

                if (i == 1)
                    item.VendasPorEstado = VendasPorEstado;

            }

            dblocal.Database.Connection.Close();

            return query;
        }

        private string GetSQLProcutividade(int id, DateTime? datainicial, DateTime? datafinal)
        {
            return " SELECT ((SUM(XX.TEMPOEMLINHA) + COALESCE(IF('N'= 'S', SUM(XX.TEMPO_PAUSA_PROD), 0),0)) / SUM(XX.TEMPO_LOGADO)) * 100 AS PRODUTIVIDADE, SUM(XX.DISCADAS) DISCADAS, SUM(XX.CONTATOS) CONTATOS, AVG(XX.CONTATOS*100/XX.DISCADAS) APROVEITAMENTO, SUM(XX.PEDIDOS) PEDIDOS FROM (SELECT	TEMPO_LOGADO.TEMPO_LOGADO,	TEMPOEMLINHA.TEMPOEMLINHA,	RECEBIDAS.RECEBIDAS,	TEMPO_PAUSA.TEMPO_PAUSA,	TEMPO_PAUSA_PROD.TEMPO_PAUSA_PROD, "
                + " COALESCE((((COALESCE(CONV_1.TOTAL, 0) + COALESCE(CONV_2.TOTAL, 0)) * 100) /	(COALESCE(CONV_3.TOTAL, 0) + COALESCE(CONV_4.TOTAL, 0))), 0) AS CONVERSAO, CAST(COUNT(DISTINCT CC.CODIGO) AS CHAR) AS DISCADAS, CAST(SUM(IF(RES.ECONTATO='SIM',1,0)) AS CHAR) AS CONTATOS, CAST(SUM(IF(RES.ESUCESSO='SIM',1,0)) AS CHAR) AS SUCESSOS, CAST(SUM(IF(RES.EPEDIDO = 'SIM', 1, 0)) AS CHAR) AS PEDIDOS FROM 	operadores OPE LEFT JOIN 			  (SELECT SUM(TIME_TO_SEC(l.tempo_logado)) AS TEMPO_LOGADO, L.OPERADOR FROM  	login_ativo_receptivo l WHERE 	L.modulo = 'Ativo' "
                + " AND DATE(entrada) between '" + String.Format("{0:yyyy-MM-dd}", datainicial) + "' AND '" + String.Format("{0:yyyy-MM-dd}", datafinal) 
                + "' GROUP BY 	L.OPERADOR) TEMPO_LOGADO ON TEMPO_LOGADO.OPERADOR = OPE.CODIGO LEFT JOIN ( SELECT SUM(TIME_TO_SEC(TIMEDIFF(AA.data_hora_fim, AA.data_hora_lig))) AS TEMPOEMLINHA,	AA.OPERADOR_LIGACAO FROM 	( SELECT  	data_hora_fim,  	data_hora_lig,  	OPERADOR_LIGACAO FROM  	campanhas_clientes WHERE "
                + " DATE(data_hora_lig) between '" + String.Format("{0:yyyy-MM-dd}", datainicial) + "' AND '" + String.Format("{0:yyyy-MM-dd}", datafinal) 
                + "' UNION ALL SELECT cr.LIGACAO_FINALIZADA, cr.LIGACAO_RECEBIDA, cr.operador FROM chamadas_receptivo cr WHERE "
                + " DATE(cr.LIGACAO_RECEBIDA) between '" + String.Format("{0:yyyy-MM-dd}", datainicial) + "' AND '" + String.Format("{0:yyyy-MM-dd}", datafinal) 
                + "' ) AA GROUP BY AA.OPERADOR_LIGACAO) TEMPOEMLINHA ON	TEMPOEMLINHA.OPERADOR_LIGACAO = OPE.CODIGO LEFT JOIN ( SELECT SUM(ligacoes_ok) AS RECEBIDAS, L.OPERADOR FROM login_ativo_receptivo l WHERE modulo = 'Receptivo' AND " 
                + " DATE(entrada) between '" + String.Format("{0:yyyy-MM-dd}", datainicial) + "' AND '" + String.Format("{0:yyyy-MM-dd}", datafinal) 
                + "' GROUP BY L.OPERADOR) RECEBIDAS ON	RECEBIDAS.OPERADOR = OPE.CODIGO LEFT JOIN (SELECT COUNT(1) AS TOTAL, OPERADOR_LIGACAO FROM campanhas_clientes INNER JOIN RESULTADOS R ON 				CAMPANHAS_CLIENTES.RESULTADO = R.CODIGO WHERE 			 campanhas_clientes.concluido = 'SIM' AND r.Esucesso ='SIM' AND "
                + " DATE(campanhas_clientes.data_hora_lig) between '" + String.Format("{0:yyyy-MM-dd}", datainicial) + "' AND '" + String.Format("{0:yyyy-MM-dd}", datafinal) 
                + "'  GROUP BY OPERADOR_LIGACAO) CONV_1 ON	CONV_1.OPERADOR_LIGACAO = OPE.CODIGO LEFT JOIN (SELECT COUNT(1) AS TOTAL,	CR.OPERADOR FROM 	chamadas_receptivo cr INNER JOIN resultados r ON	R.CODIGO = CR.RESULTADO WHERE  R.ESUCESSO = 'SIM' AND "
                + " DATE(cr.LIGACAO_RECEBIDA) between '" + String.Format("{0:yyyy-MM-dd}", datainicial) + "' AND '" + String.Format("{0:yyyy-MM-dd}", datafinal) 
                + "' GROUP BY CR.OPERADOR) CONV_2 ON	CONV_2.OPERADOR = CONV_1.OPERADOR_LIGACAO LEFT JOIN (SELECT COUNT(1) AS TOTAL, OPERADOR_LIGACAO FROM campanhas_clientes INNER JOIN resultados r ON 	R.CODIGO = resultado WHERE  campanhas_clientes.concluido = 'SIM' AND r.econtato ='SIM' AND "
                + " DATE(campanhas_clientes.data_hora_lig) between '" + String.Format("{0:yyyy-MM-dd}", datainicial) + "' AND '" + String.Format("{0:yyyy-MM-dd}", datafinal) 
                + "'  GROUP BY OPERADOR_LIGACAO) CONV_3 ON	CONV_3.OPERADOR_LIGACAO = OPE.CODIGO LEFT JOIN (SELECT COUNT(1) AS TOTAL, CR.OPERADOR FROM chamadas_receptivo cr INNER JOIN resultados r ON R.CODIGO = CR.RESULTADO WHERE  r.ECONTATO = 'SIM' AND "
                + " DATE(cr.LIGACAO_RECEBIDA) between '" + String.Format("{0:yyyy-MM-dd}", datainicial) + "' AND '" + String.Format("{0:yyyy-MM-dd}", datafinal) 
                + "'  GROUP BY CR.OPERADOR) CONV_4 ON	CONV_4.OPERADOR = OPE.CODIGO LEFT JOIN (SELECT SUM(TIME_TO_SEC(TIMEDIFF(p.DATA_HORA_FIM, p.DATA_HORA))) AS TEMPO_PAUSA,	P.OPERADOR FROM	pausas_realizadas p INNER JOIN motivos_pausa pp ON 	pp.CODIGO = p.COD_PAUSA WHERE " 
                + " DATE(p.DATA_HORA) between '" + String.Format("{0:yyyy-MM-dd}", datainicial) + "' AND '" + String.Format("{0:yyyy-MM-dd}", datafinal) 
                + "' GROUP BY P.OPERADOR	) TEMPO_PAUSA ON TEMPO_PAUSA.OPERADOR = OPE.CODIGO LEFT JOIN (SELECT SUM(IF(pp.TEMPO_MAX_SEG < (TIME_TO_SEC(TIMEDIFF(p.DATA_HORA_FIM, p.DATA_HORA))), pp.TEMPO_MAX_SEG, (TIME_TO_SEC(TIMEDIFF(p.DATA_HORA_FIM, p.DATA_HORA))))) AS TEMPO_PAUSA_PROD,	P.OPERADOR FROM	pausas_realizadas p INNER JOIN motivos_pausa pp ON pp.CODIGO = p.COD_PAUSA WHERE "
                + " DATE(p.DATA_HORA) BETWEEN '" + String.Format("{0:yyyy-MM-dd}", datainicial) + "' AND '" + String.Format("{0:yyyy-MM-dd}", datafinal) 
                + "' AND pp.PRODUTIVIDADE = 'SIM' GROUP BY 	P.OPERADOR) TEMPO_PAUSA_PROD ON "
+" TEMPO_PAUSA_PROD.OPERADOR = OPE.CODIGO "
+ " INNER JOIN(SELECT COUNT(DISTINCT CAST(l.ENTRADA AS DATE)) AS DIAS_TRABALHO, L.OPERADOR "
+ " FROM login_ativo_receptivo l "
+ " WHERE l.MODULO = 'Ativo' AND DATE(l.ENTRADA) between '" + String.Format("{0:yyyy-MM-dd}", datainicial) + "' AND '" + String.Format("{0:yyyy-MM-dd}", datafinal) + "' "
+ " GROUP BY L.OPERADOR) AS DIAS_TRABALHO ON DIAS_TRABALHO.OPERADOR = OPE.CODIGO "
+ " INNER JOIN CAMPANHAS_CLIENTES CC ON CC.OPERADOR_LIGACAO = OPE.CODIGO AND date(CC.data_hora_lig) between '" + String.Format("{0:yyyy-MM-dd}", datainicial) + "' AND '" + String.Format("{0:yyyy-MM-dd}", datafinal) + "' "
+ " INNER JOIN RESULTADOS RES ON RES.CODIGO = CC.RESULTADO "
+ " INNER JOIN CAMPANHAS CAM ON CAM.CODIGO = CC.CAMPANHA "
+ " WHERE OPE.CODIGO = "+id.ToString()+") XX  ";
        }

        private void GetValores(int id, DateTime? datainicial, DateTime? datafinal)
        {
            if (datainicial == null)
                datainicial = DateTime.Now;

            if (datafinal == null)
                datafinal = DateTime.Now;
                                    
            FuncoesBanco f = new FuncoesBanco(dblocal);
            
            var x = GetSQLProcutividade(id, datainicial, datafinal);
                        
            List<dynamic> MyList = f.CollectionFromSql(x,
               new Dictionary<string, object> { }).ToList();

            foreach (dynamic item in MyList)
            {
                if (!DBNull.Equals(item.PRODUTIVIDADE, DBNull.Value))
                    PRODUTIVIDADE = Convert.ToDouble(item.PRODUTIVIDADE);
                else
                    PRODUTIVIDADE = 0;

                if (!DBNull.Equals(item.DISCADAS, DBNull.Value))
                    DISCADAS = Convert.ToInt32(item.DISCADAS);
                else
                    DISCADAS = 0;
                if (!DBNull.Equals(item.CONTATOS, DBNull.Value))
                    CONTATOS = Convert.ToInt32(item.CONTATOS);
                else
                    CONTATOS = 0;
                if (!DBNull.Equals(item.APROVEITAMENTO, DBNull.Value))
                    APROVEITAMENTO = Convert.ToDouble(item.APROVEITAMENTO);
                else
                    APROVEITAMENTO = 0;
                if (!DBNull.Equals(item.PEDIDOS, DBNull.Value))
                    PEDIDOS = Convert.ToInt32(item.PEDIDOS);
                else
                    PEDIDOS = 0;
            }

            x = "SELECT cli.ESTADO, SUM(cc.VALOR) as VALOR FROM compras cc "
              + " JOIN clientes cli on cli.CODIGO = cc.CLIENTE AND cli.ESTADO <> '' "
              + " WHERE cc.OPERADOR > 0 AND cc.DATA BETWEEN '" + String.Format("{0:yyyy-MM-dd}", datainicial) 
              + "' AND '" + String.Format("{0:yyyy-MM-dd}", datafinal) + "' "
              + " group by cli.ESTADO having SUM(cc.VALOR) > 0 ORDER BY COUNT(cc.CODIGO) DESC ";

            if (VendasPorEstado == null)
            {
                MyList = f.CollectionFromSql(x,
                   new Dictionary<string, object> { }).ToList();

                VendasPorEstado = MyList;
            }

            x = "SELECT meta.OPERADOR,o.LOGIN, sum(meta.VALOR_META) as META "
            + " FROM operadores_meta meta JOIN operadores o on o.CODIGO = meta.OPERADOR WHERE CAST(CONCAT(meta.ANO, '-', meta.MES, '-01') AS DATE) BETWEEN '" + String.Format("{0:yyyy-MM-dd}", datainicial) + "' AND '" + String.Format("{0:yyyy-MM-dd}", datafinal) + "' "
            + " group by meta.OPERADOR,o.LOGIN ";

            if (MetasXVendas == null)
            {
                MyList = f.CollectionFromSql(x,
                   new Dictionary<string, object> { }).ToList();

                MetasXVendas = MyList;

                foreach(var m in MetasXVendas)
                {
                    x = "SELECT SUM(cc.VALOR) as VALOR FROM compras cc "
                     + " WHERE cc.OPERADOR = "+ m.OPERADOR + " AND cc.DATA BETWEEN '" + String.Format("{0:yyyy-MM-dd}", datainicial)
                     + "' AND '" + String.Format("{0:yyyy-MM-dd}", datafinal) + "' ";

                    var valor = f.ExecSql(x);

                    if (valor != null && valor.Count > 0)
                        m.VALOR_VENDA = Convert.ToDouble(valor[0]);
                    else
                        m.VALOR_VENDA = 0;

                    x = " select p.VALOR from campanhas_clientes a inner join propostas p on p.LIGACAO = a.CODIGO "
                     + " WHERE a.OPERADOR = " + m.OPERADOR + " AND a.DT_RESULTADO BETWEEN '" + String.Format("{0:yyyy-MM-dd}", datainicial)
                     + "' AND '" + String.Format("{0:yyyy-MM-dd}", datafinal) + "' ";
                    valor = f.ExecSql(x);

                    if (valor != null && valor.Count > 0)
                        m.VALOR_PROPOSTA = Convert.ToDouble(valor[0]) ;
                    else
                        m.VALOR_PROPOSTA = 0;
                }

            }
        }

        [Route("api/dashboard/localizar")]
        [HttpGet]
        public IHttpActionResult Localizar([FromUri]filtros filtros)
        {
            var ope = db.Set<v_operadores_status>().ToList();
            var i = 0;

            foreach (var item in ope)
            {
                filtros.DATAINICIAL = filtros.DATAINICIAL.Substring(0, 10);
                filtros.DATAFINAL = filtros.DATAFINAL.Substring(0, 10);

                DateTime DataInicial = DateTime.ParseExact(filtros.DATAINICIAL, "yyyy-MM-dd",
                                       System.Globalization.CultureInfo.InvariantCulture);
                DateTime DataFinal = DateTime.ParseExact(filtros.DATAFINAL, "yyyy-MM-dd",
                                       System.Globalization.CultureInfo.InvariantCulture);


                i++;
                if (filtros != null)
                    GetValores(item.id, DataInicial, DataFinal);
                else
                    GetValores(item.id, null, null);

                item.APROVEITAMENTO = APROVEITAMENTO;
                item.CONTATOS = CONTATOS;
                item.LIGACOES = DISCADAS;
                item.PRODUTIVIDADE = PRODUTIVIDADE;
                item.PEDIDOS = PEDIDOS;

                if (i == 1)
                {
                    item.VendasPorEstado = VendasPorEstado;
                    item.MetasXVendas = MetasXVendas;
                }

                var foto = dblocal.Set<operadores_foto>().Where(q => q.id == item.id).FirstOrDefault();

                if (foto != null)
                {
                    var diretorio = HttpContext.Current.Server.MapPath("~") + @"\img\" + item.id.ToString() + ".png";
                    item.FOTO =  item.id.ToString() + ".png";
                    var f = byteArrayToImage(foto.FOTO);
                    f.Save(diretorio);
                }
            }

            dblocal.Database.Connection.Close();

            if (ope == null)
            {
                return NotFound();
            }

            return Ok(ope);
        }

        public Image byteArrayToImage(byte[] byteArrayIn)
        {
            MemoryStream ms = new MemoryStream(byteArrayIn);
            Image returnImage = Image.FromStream(ms);
            return returnImage;
        }

    }

    public class filtros
    {
        public string DATAINICIAL { get; set; }
        public string DATAFINAL { get; set; }
    }
    
}