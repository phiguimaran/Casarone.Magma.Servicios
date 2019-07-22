Imports Magma.Tools.Core.Common.DataServices
Imports Magma.Tools.Core.Business.DataServices
Imports Uruware.LibUcfe.Xml
Imports LiquidTechnologies.Runtime.Net40

Class CFEXML
    ' otra prueba de cambios
    Public cfe As New Uruware.LibUcfe.Xml.CFE()
    Public codComercio As String
    Public codTerminal As String
    Public adenda As String
    Public emailReceptor As String

    Function Almacenar(ByRef _transaccion As System.Data.IDbTransaction, ByVal respBody As serviciocfe.RespBody, ByVal tag As String, ByVal tipoCFE As String) As Integer

        '---------------------------------
        ' acá almaceno los datos retornados por el servicio en una tabla local asociada por nro_trans a la tabla de facturas
        Dim sqlParams As New SQLParameters(_transaccion)

        Dim cmd As New SQLCommand
        cmd.Text = "select count(nro_trans) from edocumentos where nro_trans=" & respBody.Resp.Uuid
        If SQL.SelScalar(cmd, sqlParams) = 0 Then
            cmd.Text = "INSERT INTO edocumentos(nro_trans, str_qr, serie_cfe, nro_cfe, numero_cae, rango_cae_desde, rango_cae_hasta, fec_venc, cod_seguridad, tipo_cfe, tag, codrta, mensajerta, formulario, seccion,bloque,linea,usuario_mod,fecha_mod,terminal_mod,operacion_mod,estado_registro) VALUES ("
            cmd.Text = cmd.Text & "" & respBody.Resp.Uuid
            cmd.Text = cmd.Text & ",'" & respBody.Resp.DatosQr & "'"
            cmd.Text = cmd.Text & ",'" & respBody.Resp.Serie & "'"
            cmd.Text = cmd.Text & "," & respBody.Resp.NumeroCfe
            cmd.Text = cmd.Text & "," & respBody.Resp.IdCae
            cmd.Text = cmd.Text & "," & respBody.Resp.CaeNroDesde
            cmd.Text = cmd.Text & "," & respBody.Resp.CaeNroHasta
            cmd.Text = cmd.Text & ",'" & respBody.Resp.VencimientoCae.Substring(0, 4) & "-" & respBody.Resp.VencimientoCae.Substring(4, 2) & "-" & respBody.Resp.VencimientoCae.Substring(6, 2) & " 00:00:00'"
            cmd.Text = cmd.Text & ",'" & respBody.Resp.CodigoSeguridad & "'"
            cmd.Text = cmd.Text & ",'" & respBody.Resp.TipoCfe & "'"
            cmd.Text = cmd.Text & ",'" & tag & "'"
            cmd.Text = cmd.Text & ",'" & respBody.Resp.CodRta & "'"
            cmd.Text = cmd.Text & ",'" & IIf(respBody.Resp.MensajeRta Is Nothing, "", respBody.Resp.MensajeRta) & "'"
            cmd.Text = cmd.Text & ",'imprcasarone','seccion','bloque',1,'informix',current,'TER','Nuevo','A')"
            SQL.EjeCmd(cmd.Text) 'lo hago sin la transación para que quede grabado aún si el formulario hace un rollback, ya que en realidad el comprobante ya fué enviado a dgi. de esta forma me queda algun registro de que llegó hasta acá y luego se cayó

            cmd.Text = "INSERT INTO edoclog(nro_trans, log) VALUES (" & respBody.Resp.Uuid & ",'X01-ya insertó en edocumentos')"
            SQL.EjeCmd(cmd.Text) 'lo hago sin la transación para que quede grabado aún si el formulario hace un rollback, ya que en realidad el comprobante ya fué enviado a dgi. de esta forma me queda algun registro de que llegó hasta acá y luego se cayó

            Select Case tipoCFE
                Case "101", "111"   ' factura o tiket
                    cmd.Text = "UPDATE cpt_facturas set serie_cfe ='" & respBody.Resp.Serie & "', nro_cfe=" & respBody.Resp.NumeroCfe & ", nro_doc=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams) ' esto lo tengo que hacer si o si con la transaccion porque es un registro nuevo que solo existe dentro de la misma, sino no va a encontrar filas para actualizar
                    cmd.Text = "UPDATE cpt_facturas_lin set nro_doc=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "UPDATE cpf_stock set nro_doc=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "UPDATE cps_pedidos set nro_doc=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "UPDATE cpf_movcontdir set nro_doc=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "UPDATE cpf_impsimples set nro_doc=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "UPDATE cps_pedidosentr set nro_doc=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "UPDATE cpt_cabgenac set nro_doc=" & respBody.Resp.NumeroCfe & ",nro_int=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "UPDATE cps_ctasctes set nro_doc=" & respBody.Resp.NumeroCfe & ",nro_doca=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "UPDATE cps_notasent_lin set nro_doc=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    'si tipo_entrega <> 'entrega con esta factura' y el nro_nota_ent is null --> actualizo nro_doca = numerocfe
                    cmd.Text = "UPDATE cps_notasent_lin set nro_doca=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid & " and linea = (select L.linea from cpt_facturas C, cpt_facturas_lin L where C.nro_trans = L.nro_trans and C.tipo_entrega <> 'ENTREGA CON ESTA FACTURA' and L.nro_nota_ent is null and C.nro_trans = " & respBody.Resp.Uuid & ")"
                    SQL.EjeCmd(cmd, sqlParams)
                    ' Hasta aquí son comunes entre factura crédito y contado. En adelante son exclusivos de la factura contado.
                    cmd.Text = "UPDATE cpf_efectivo set nro_doc=" & respBody.Resp.NumeroCfe & ",nro_int=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "UPDATE cps_valores set nro_doc=" & respBody.Resp.NumeroCfe & ",nro_int=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "UPDATE cpf_movbcos set nro_doc=" & respBody.Resp.NumeroCfe & ",nro_int=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                Case "102", "112"   ' NC o NC tiket
                    cmd.Text = "UPDATE cpt_notascred set serie_cfe ='" & respBody.Resp.Serie & "', nro_cfe=" & respBody.Resp.NumeroCfe & ", nro_doc=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "UPDATE cpt_notascre_lin set nro_doc=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "UPDATE cpf_movcontdir set nro_doc=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "UPDATE cpf_impsimples set nro_doc=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "UPDATE cpt_cabgenac set nro_doc=" & respBody.Resp.NumeroCfe & ",nro_int=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "UPDATE cps_ctasctes set nro_doc=" & respBody.Resp.NumeroCfe & ",nro_doca=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    ' Hasta aquí son comunes entre NC crédito y contado. En adelante son exclusivos de la NC contado.
                    cmd.Text = "UPDATE cpf_efectivo set nro_doc=" & respBody.Resp.NumeroCfe & ",nro_int=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "UPDATE cps_valores set nro_doc=" & respBody.Resp.NumeroCfe & ",nro_int=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "UPDATE cpf_movbcos set nro_doc=" & respBody.Resp.NumeroCfe & ",nro_int=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                Case "121"   ' factura exportacion
                    cmd.Text = "update cpt_facturas_exp set serie_cfe ='" & respBody.Resp.Serie & "', nro_cfe=" & respBody.Resp.NumeroCfe & ", nro_doc=" & respBody.Resp.NumeroCfe & " where nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams) ' esto lo tengo que hacer si o si con la transaccion porque es un registro nuevo que solo existe dentro de la misma, sino no va a encontrar filas para actualizar
                    cmd.Text = "update cpf_movcontdir set nro_doc=" & respBody.Resp.NumeroCfe & " where nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "update cps_ctasctes set nro_doc=" & respBody.Resp.NumeroCfe & ",nro_doca=" & respBody.Resp.NumeroCfe & " where nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    '           en la cabgenac pone el nro_doc = nro trans --- no se poque y nro_int = nro_doc
                    cmd.Text = "update cpt_cabgenac set nro_int=" & respBody.Resp.NumeroCfe & " where nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    ' esto ultimo queda comentado
                    '           no se porque pero el nro_doc no se graba en las lineas, por ahora lo dejo asi
                    '        cmd.Text = "update cpt_factexp_lin set nro_doc=" & respBody.Resp.NumeroCfe & " where nro_trans=" & respBody.Resp.Uuid
                    '        SQL.EjeCmd(cmd, sqlParams)
                Case "122"   ' nc de exportacion
                    cmd.Text = "update cpt_notascre_exp set serie_cfe ='" & respBody.Resp.Serie & "', nro_cfe=" & respBody.Resp.NumeroCfe & ", nro_doc=" & respBody.Resp.NumeroCfe & " where nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "update cpf_movcontdir set nro_doc=" & respBody.Resp.NumeroCfe & " where nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "update cps_ctasctes set nro_doc=" & respBody.Resp.NumeroCfe & ",nro_doca=" & respBody.Resp.NumeroCfe & " where nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    'en el caso de las NC si pone en la cabgenac el nro_doc = nro_doc
                    cmd.Text = "update cpt_cabgenac set nro_int=" & respBody.Resp.NumeroCfe & ",nro_doc=" & respBody.Resp.NumeroCfe & " where nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                Case "124" ' rex remito de exportacion
                    cmd.Text = "update cpt_notas_ent set serie_cfe ='" & respBody.Resp.Serie & "', nro_cfe=" & respBody.Resp.NumeroCfe & ", nro_doc=" & respBody.Resp.NumeroCfe & " where nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "update cpt_notasent_lin set nro_doc=" & respBody.Resp.NumeroCfe & " where nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "update cpf_stock set nro_doc=" & respBody.Resp.NumeroCfe & " where nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "update cps_notasent_lin set nro_doc=" & respBody.Resp.NumeroCfe & " where nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "update cpt_pesadas set nro_doc=" & respBody.Resp.NumeroCfe & " where nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                Case "151", "152" ' eBoleta de entrada
                    '*********** 201806 P.H.
                    Dim strcoddoc As String = IIf(tipoCFE = "151", "bolc", "bold")
                    cmd.Text = "UPDATE cpt_nc_prod_cab set serie_cfe ='" & respBody.Resp.Serie & "', cod_doc='" & strcoddoc.Trim & "', nro_doc=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "UPDATE cpt_nc_prod_lin set serie_cfe ='" & respBody.Resp.Serie & "', cod_doc='" & strcoddoc.Trim & "', nro_doc=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "UPDATE cpf_movcontdir set cod_doc='" & strcoddoc.Trim & "', nro_doc=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                    cmd.Text = "UPDATE cps_ctasctes set cod_doc='" & strcoddoc.Trim & "', cod_doca='" & strcoddoc.Trim & "', cod_doc_ref='" & strcoddoc.Trim & "', nro_doc=" & respBody.Resp.NumeroCfe & ",nro_doca=" & respBody.Resp.NumeroCfe & ",nro_doc_ref=" & respBody.Resp.NumeroCfe & " WHERE nro_trans=" & respBody.Resp.Uuid
                    SQL.EjeCmd(cmd, sqlParams)
                Case "224" ' rexc para los de contingencia no tengo que hacer nada ya que el numero y serie fueron ingresados por el usuario
                Case Else
                    cmd.Text = "INSERT INTO edoclog(nro_trans, log) VALUES (" & respBody.Resp.Uuid & ",'X015-tipo de documento no contemplado')"
                    SQL.EjeCmd(cmd.Text) 'lo hago sin la transación para que quede grabado aún si el formulario hace un rollback, ya que en realidad el comprobante ya fué enviado a dgi. de esta forma me queda algun registro de que llegó hasta acá y luego se cayó
                    Return 10 ' tipo de comprobante no contemplado
            End Select

            cmd.Text = "INSERT INTO edoclog(nro_trans, log) VALUES (" & respBody.Resp.Uuid & ",'X02-ya pasó el case y modificó todas las tablas relacionadas')"
            SQL.EjeCmd(cmd.Text) 'lo hago sin la transación para que quede grabado aún si el formulario hace un rollback, ya que en realidad el comprobante ya fué enviado a dgi. de esta forma me queda algun registro de que llegó hasta acá y luego se cayó
        Else
            ' si ya existía el registro es porque es una edición, actualizo solo el campo tag que es para uso interno
            cmd.Text = "update edocumentos set tag = '" & tag & "' where nro_trans = " & respBody.Resp.Uuid
            SQL.EjeCmd(cmd.Text)

            cmd.Text = "INSERT INTO edoclog(nro_trans, log) VALUES (" & respBody.Resp.Uuid & ",'EDIT- extraño, no se en que situaciones se puede dar esto, buscar el codigo dlfvhpqww en las fuentes')"
            SQL.EjeCmd(cmd.Text) 'lo hago sin la transación para que quede grabado aún si el formulario hace un rollback, ya que en realidad el comprobante ya fué enviado a dgi. de esta forma me queda algun registro de que llegó hasta acá y luego se cayó
        End If
        sqlParams = Nothing
        cmd = Nothing
        Return 0
    End Function

    Public Function Invocar(ByVal tipoMensaje As Integer, ByVal tipoCFE As String, ByVal trans As String, ByRef respBody As serviciocfe.RespBody) As Integer
        Dim ucfe As New serviciocfe.CfeServiceClient
        Dim cuerpoReq As New serviciocfe.ReqBody
        Dim Req As New serviciocfe.RequerimientoParaUcfe
        Dim auxdatetime As DateTime

        '--- cargo datos del requerimiento
        auxdatetime = DateTime.Now
        Req.TipoMensaje = tipoMensaje   ' Solicitud de firma de CFE
        Req.Uuid = trans  ' identificador del documento =  nro_trans
        Req.TipoCfe = tipoCFE
        Req.IdReq = "1"
        Req.HoraReq = Format(auxdatetime, "hhmmss")
        Req.FechaReq = Format(auxdatetime, "yyyyMMdd")
        Req.CodTerminal = Me.codTerminal.Trim
        Req.CodComercio = Me.codComercio.Trim
        Req.CfeXmlOTexto = Me.cfe.ToXml()
        Req.Adenda = Me.adenda.ToString
        Req.EmailEnvioPdfReceptor = Me.emailReceptor
        'Throw New Exception(Req.CfeXmlOTexto)

        '--- armo el parametro a pasarle al servicio
        cuerpoReq.Req = Req
        cuerpoReq.RequestDate = Format(auxdatetime, "yyyy-MM-dd hh:mm:ss")
        cuerpoReq.Tout = 3000
        cuerpoReq.CodTerminal = Me.codTerminal.Trim
        cuerpoReq.CodComercio = Me.codComercio.Trim

        '--- invoco al servicio
        Try
            respBody = ucfe.Invoke(cuerpoReq)  'llamo al servicio y tengo la respuesta
        Catch
            Return 501
        End Try

        If respBody.ErrorCode <> 0 Then Return respBody.ErrorCode 'errores de procesamiento 100-500
        If Val(respBody.Resp.CodRta) <> 0 Then Return Val(respBody.Resp.CodRta) 'respuesta con error 1-99
        ucfe = Nothing
        cuerpoReq = Nothing
        Req = Nothing
        Return 0    'todo ok
    End Function

    Public Function ObtenerDatos(ByVal _transaccion As System.Data.IDbTransaction, ByVal tipoCFE As String, ByVal trans As String) As Integer
        If tipoCFE = "101" Or tipoCFE = "102" Or tipoCFE = "111" Or tipoCFE = "112" Or tipoCFE = "151" Or tipoCFE = "152" Then
            Return ObtenerDatosPlaza(_transaccion, tipoCFE, trans)
        ElseIf tipoCFE = "121" Or tipoCFE = "122" Or tipoCFE = "124" Or tipoCFE = "224" Then
            Return ObtenerDatosExport(_transaccion, tipoCFE, trans)
        Else
            Return 10 ' tipo de comprobante no implementado
        End If
    End Function

    Public Function ObtenerDatosPlaza(ByVal _transaccion As System.Data.IDbTransaction, ByVal tipoCFE As String, ByVal trans As String) As Integer
        Dim cab As System.Data.DataTable
        Dim lin As System.Data.DataTable
        Dim otr As System.Data.DataTable
        Dim otr2 As System.Data.DataTable
        Dim i, totLineas As Integer
        Dim totMontoNF, totMontoNG, totMontoExp, totMontoTM, totMontoTB, totIvaTM, totIvaTB As Double
        Dim cuentasExportAsim As New List(Of Integer)({301, 313})
        Dim fpagoContado As Integer = 101  ' CODIGO DE FORMA DE PAGO QUE REPRESENTA EL PAGO CONTADO
        Dim sqlCab, sqlLin As String
        Dim sqlParams As New SQLParameters(_transaccion)
        Dim x As New SQLCommand
        Dim fechaAux As Date
        Dim micfe

        x.Text = "select valor_param_char from ct_paramxemp where cod_emp = 'casa' and cod_parametro = 'codcomercio'"
        otr = SQL.SelDataTable(x, sqlParams)
        If otr.Rows.Count <> 1 Then Return 3 ' no se encontró parametro codcomercio
        Me.codComercio = otr.Rows(0).Item("valor_param_char")
        ' Para Testing
        'Me.codComercio = "Casaro-1"

        x.Text = "select valor_param_char from ct_paramxemp where cod_emp = 'casa' and cod_parametro = 'codterminal'"
        otr = SQL.SelDataTable(x, sqlParams)
        If otr.Rows.Count <> 1 Then Return 4 ' no se encontró parametro codterminal
        Me.codTerminal = otr.Rows(0).Item("valor_param_char")
        ' Para Testing
        'Me.codTerminal = "FC-1"

        If tipoCFE = "101" Or tipoCFE = "111" Then 'tipoCFE = 101 0 111 - Facturas
            ' tomo datos del cabezal de la factura
            sqlCab = "select CAB.fec_doc, CAB.cod_fpago, CAB.fec_vto_fac, CAB.cod_sucursal, CAB.tipo_documento, CAB.cod_pais, "
            sqlCab += " CAB.nro_dgi, CAB.nom_tit, CAB.dir_tit, CAB.ciudad_tit, MON.id_sopmg_mone, CAB.tc_ing, (select porc_impuesto from ct_impuestos where cod_tipo_impu = "
            sqlCab += "'IVA' and cod_tasa_impu = '10') iva_minima, (select porc_impuesto from ct_impuestos where cod_tipo_impu = 'IVA' and cod_tasa_impu = '22') iva_basica, CAB.estado_registro, CAB.formulario, CAB.redondeo, "
            sqlCab += " (select nvl(mail,'') from ct_mailefact where cod_tit = TIT.cod_tit) emailReceptor "
            sqlCab += " FROM cpt_facturas CAB,  ct_titulares TIT,  ct_monedas MON "
            sqlCab += " WHERE  CAB.cod_tit = TIT.cod_tit and CAB.cod_moneda = MON.cod_moneda and CAB.nro_trans = " & trans
            x.Text = sqlCab
            cab = SQL.SelDataTable(x, sqlParams)
            If cab.Rows.Count = 0 Then Return 5 ' no se encontró el cabezal de la factura
            ' tomo datos de las lineas de la factura
            sqlLin = "SELECT LIN.linea, LIN.nom_det_art, LIN.cantidad, LIN.cod_unidad, LIN.bonif_com, LIN.cta_vta_fac, LIN.porc_iva, LIN.total_linea, "
            sqlLin += " LIN.imp_iva,LIN.precio,LIN.cod_articulo FROM cpt_facturas_lin LIN, ct_articulos ART WHERE lin.cod_articulo = ART.cod_articulo And lin.nro_trans = " & trans
            x.Text = sqlLin
            lin = SQL.SelDataTable(x, sqlParams)
            If lin.Rows.Count = 0 Then Return 6 ' no se encontraron las lineas de la fatura
        ElseIf tipoCFE = "102" Or tipoCFE = "112" Then 'tipoCFE = 102 o 112 - Notas de credito
            sqlCab = "select CAB.fec_doc, CAB.cod_fpago, CAB.fec_vto_fac, CAB.cod_sucursal, CAB.tipo_documento, CAB.cod_pais, "
            sqlCab += " CAB.nro_dgi, CAB.nom_tit, CAB.dir_tit, CAB.ciudad_tit, MON.id_sopmg_mone, CAB.tc_ing, (select porc_impuesto from ct_impuestos where cod_tipo_impu = "
            sqlCab += "'IVA' and cod_tasa_impu = '10') iva_minima, (select porc_impuesto from ct_impuestos where cod_tipo_impu = 'IVA' and cod_tasa_impu = '22') iva_basica, CAB.estado_registro, CAB.formulario, CAB.redondeo, CAB.nro_factura, CAB.cod_doc_fac, CAB.serie_factura, "
            sqlCab += " (select nvl(mail,'') from ct_mailefact where cod_tit = TIT.cod_tit) emailReceptor "
            sqlCab += " FROM cpt_notascred CAB, ct_titulares TIT, ct_monedas MON "
            sqlCab += " WHERE  CAB.cod_tit = TIT.cod_tit and CAB.cod_moneda = MON.cod_moneda and CAB.nro_trans = " & trans
            x.Text = sqlCab
            cab = SQL.SelDataTable(x, sqlParams)
            If cab.Rows.Count = 0 Then Return 5 ' no se encontró el cabezal de la factura
            sqlLin = "SELECT LIN.linea, LIN.nom_det_art, LIN.cantidad, LIN.cod_unidad, LIN.bonif_com, LIN.cta_vta_fac, LIN.porc_iva, LIN.total_linea, "
            sqlLin += " LIN.imp_iva,LIN.precio,LIN.cod_articulo FROM cpt_notascre_lin LIN, ct_articulos ART WHERE lin.cod_articulo = ART.cod_articulo And lin.nro_trans = " & trans
            x.Text = sqlLin
            lin = SQL.SelDataTable(x, sqlParams)
            If lin.Rows.Count = 0 Then Return 6 ' no se encontraron las lineas de la fatura
        ElseIf tipoCFE = "151" Or tipoCFE = "152" Then 'tipoCFE = 151 o 152 - Boleta de compra
            ' tomo datos del cabezal de la factura
            sqlCab = "select CAB.fec_doc, '101' cod_fpago, CAB.fec_doc fec_vto_fac, 1 cod_sucursal, TIT.tipo_documento, TIT.cod_pais, "
            sqlCab += " TIT.nro_dgi, TIT.nom_tit, TIT.dir_tit, TIT.ciudad_tit, MON.id_sopmg_mone, CAB.tc_ing, (select porc_impuesto from ct_impuestos where cod_tipo_impu = "
            sqlCab += " 'IVA' and cod_tasa_impu = '10') iva_minima, (select porc_impuesto from ct_impuestos where cod_tipo_impu = 'IVA' and cod_tasa_impu = '22') iva_basica, CAB.estado_registro, CAB.formulario, 0 redondeo, CAB.cod_tit, CAB.nro_trans, "
            sqlCab += " (select nvl(mail,'') from ct_mailefact where cod_tit = TIT.cod_tit) emailReceptor "
            sqlCab += " FROM cpt_nc_prod_cab CAB,  cpf_movcontdir CONT, ct_titulares TIT,  ct_monedas MON "
            sqlCab += " WHERE CAB.nro_trans = CONT.nro_trans and CAB.cod_tit = TIT.cod_tit and CONT.cod_moneda_ing = MON.cod_moneda and CAB.nro_trans = " & trans
            x.Text = sqlCab
            cab = SQL.SelDataTable(x, sqlParams)
            If cab.Rows.Count = 0 Then Return 5 ' no se encontró el cabezal de la factura
            ' tomo datos de las lineas de la factura
            sqlLin = "SELECT 1 linea, CAB.observaciones nom_det_art, sum(LIN.kilos_limpios) cantidad, 'KG' cod_unidad, 0 bonif_com, '' cta_vta_fac, "
            sqlLin += " 0 porc_iva, abs(sum(imp_bonif)) total_linea, 0 imp_iva, abs(sum(imp_bonif)/sum(LIN.kilos_limpios)) precio, 'N/A' cod_articulo "
            sqlLin += " FROM cpt_nc_prod_lin LIN, cpt_nc_prod_cab CAB "
            sqlLin += " WHERE LIN.nro_trans = CAB.nro_trans and lin.nro_trans = " & trans
            sqlLin += " group by 2 "
            x.Text = sqlLin
            lin = SQL.SelDataTable(x, sqlParams)
            If lin.Rows.Count = 0 Then Return 6 ' no se encontraron las lineas de la fatura
        End If
        Me.emailReceptor = cab.Rows(0).Item("emailReceptor").ToString.Trim
        Me.emailReceptor = Strings.Replace(Me.emailReceptor, "#", "@")
        Me.emailReceptor = Strings.Replace(Me.emailReceptor, " ", "")
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ''''''''''''''''''''''''''''''''''''''''''''''''' ENCABEZADO ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        ' creo el objeto correspondiente
        If tipoCFE = "101" Then ' Factura contado se utiliza eTicket
            Me.cfe.ETck = New Uruware.LibUcfe.Xml.ETck()
            Me.cfe.ETck.Encabezado.IdDoc.TipoCFE = Enumerations.IdDoc_Tck_TipoCFE.n101
            micfe = Me.cfe.ETck
        ElseIf tipoCFE = "102" Then ' NC contado se utiliza eTicket
            Me.cfe.ETck = New Uruware.LibUcfe.Xml.ETck()
            Me.cfe.ETck.Encabezado.IdDoc.TipoCFE = Enumerations.IdDoc_Tck_TipoCFE.n102
            micfe = Me.cfe.ETck
        ElseIf tipoCFE = "111" Then ' Factura credito se utiliza eFactura
            Me.cfe.EFact = New Uruware.LibUcfe.Xml.EFact()
            Me.cfe.EFact.Encabezado.IdDoc.TipoCFE = Enumerations.IdDoc_Fact_TipoCFE.n111
            micfe = Me.cfe.EFact
        ElseIf tipoCFE = "112" Then ' NC crédito se utiliza eFactura
            Me.cfe.EFact = New Uruware.LibUcfe.Xml.EFact()
            Me.cfe.EFact.Encabezado.IdDoc.TipoCFE = Enumerations.IdDoc_Fact_TipoCFE.n112
            micfe = Me.cfe.EFact
        ElseIf tipoCFE = "151" Then ' NC crédito se utiliza eFactura
            Me.cfe.EBoleta = New Uruware.LibUcfe.Xml.EBoleta()
            Me.cfe.EBoleta.Encabezado.IdDoc.TipoCFE = Enumerations.IdDoc_Boleta_TipoCFE.n151
            micfe = Me.cfe.EBoleta
        ElseIf tipoCFE = "152" Then ' NC crédito se utiliza eFactura
            Me.cfe.EBoleta = New Uruware.LibUcfe.Xml.EBoleta()
            Me.cfe.EBoleta.Encabezado.IdDoc.TipoCFE = Enumerations.IdDoc_Boleta_TipoCFE.n152
            micfe = Me.cfe.EBoleta
        End If
        'micfe se utiliza para poder referenciar al comprobante que sea

        '''''''' Identificacion del comprobante '''''''
        fechaAux = cab.Rows(0).Item("fec_doc")   'Format(cab.Rows(0).Item("fec_doc"), "yyyyMMdd")
        micfe.Encabezado.IdDoc.FchEmis = New LiquidTechnologies.Runtime.Net40.XmlDateTime(fechaAux)
        If CInt(cab.Rows(0).Item("cod_fpago")) = fpagoContado Then
            micfe.Encabezado.IdDoc.FmaPago = Uruware.LibUcfe.Xml.Enumerations.IdDoc_Fact_FmaPago.n1
        Else
            micfe.Encabezado.IdDoc.FmaPago = Uruware.LibUcfe.Xml.Enumerations.IdDoc_Fact_FmaPago.n2
        End If


        '''''''''''''''' Emisor '''''''''''''''''''''''
        x.Text = "select valor_param_char from ct_paramxemp where cod_emp = 'casa' and cod_parametro = 'rut'"
        otr = SQL.SelDataTable(x, sqlParams)
        If otr.Rows.Count <> 1 Then Return 1 ' no se encontró parametro rut
        micfe.Encabezado.Emisor.RUCEmisor = Val(otr.Rows(0).Item("valor_param_char"))

        x.Text = "select valor_param_char from ct_paramxemp where cod_emp = 'casa' and cod_parametro = 'emisor'"
        otr = SQL.SelDataTable(x, sqlParams)
        If otr.Rows.Count <> 1 Then Return 2 ' no se encontró parametro emisor
        micfe.Encabezado.Emisor.RznSoc = otr.Rows(0).Item("valor_param_char").ToString.Trim

        x.Text = "select cod_sucursal, nom_sucursal, dir_planta, ciudad_tit, nom_depto_uy from ct_sucursales where cod_sucursal=" & cab.Rows(0).Item("cod_sucursal").ToString
        otr = SQL.SelDataTable(x, sqlParams)
        If otr.Rows.Count = 0 Then Return 7 ' no se encontraró la sucursal indicada en la factura
        micfe.Encabezado.Emisor.EmiSucursal = otr.Rows(0).Item("nom_sucursal")
        micfe.Encabezado.Emisor.CdgDGISucur = CInt(otr.Rows(0).Item("cod_sucursal"))
        micfe.Encabezado.Emisor.DomFiscal = otr.Rows(0).Item("dir_planta")
        micfe.Encabezado.Emisor.Ciudad = otr.Rows(0).Item("ciudad_tit")
        micfe.Encabezado.Emisor.Departamento = otr.Rows(0).Item("nom_depto_uy")

        '''''''''''''''''' Receptor '''''''''''''''''''
        x.Text = "select iso3166 from ct_paises where cod_pais = '" & cab.Rows(0).Item("cod_pais").ToString.Trim & "'"
        otr = SQL.SelDataTable(x, sqlParams)
        If otr.Rows.Count = 0 Then Return 8 ' no se encontraró el del país del cliente indicado en la factura
        If otr.Rows(0).Item("iso3166") = "" Then Return 9 ' no se encontraró el codigo iso 3166 el del país del cliente indicado en la factura

        ' en los etickets los datos del receptor se manejan bastante distinto que en las efacturas
        If tipoCFE = "101" Or tipoCFE = "102" Then
            ' se ve que como el receptor no siempre es obligatorio, etonces no se inicializa y hay que hacerle un New
            micfe.Encabezado.Receptor = New Uruware.LibUcfe.Xml.Receptor_Tck
            'Dim aa As New Uruware.LibUcfe.Xml.Receptor_Tck
            'A60  si es ticket pueden haber distintos tipos de documento por lo tanto hay que especificarlo
            micfe.Encabezado.Receptor.TipoDocRecep = Uruware.LibUcfe.Xml.Enumerations.DocTypeFromString(cab.Rows(0).Item("tipo_documento").ToString.Trim)
            'A61 codigo de pais
            micfe.Encabezado.Receptor.CodPaisRecep = Uruware.LibUcfe.Xml.Enumerations.CodPaisTypeFromString(otr.Rows(0).Item("iso3166"))
            'A62 y A62.1, si el tipo documento es CI uruguaya el codigo va en A62 si es cualquier otro documento de otro pais entonces va en A62.1
            micfe.Encabezado.Receptor.Receptor_Tck_Choice = New Uruware.LibUcfe.Xml.Receptor_Tck_Choice()
            If cab.Rows(0).Item("tipo_documento") = 3 Then
                micfe.Encabezado.Receptor.Receptor_Tck_Choice.DocRecep = cab.Rows(0).Item("nro_dgi").ToString.Trim
            Else
                micfe.Encabezado.Receptor.Receptor_Tck_Choice.DocRecepExt = cab.Rows(0).Item("nro_dgi").ToString.Trim
            End If
        ElseIf tipoCFE = "111" Or tipoCFE = "112" Then
            micfe.Encabezado.Receptor = New Uruware.LibUcfe.Xml.Receptor_Fact
            'A60  tipos de documento no hace falta indicarlo es siempre 2
            'micfe.Encabezado.Receptor.TipoDocRecep = Uruware.LibUcfe.Xml.Enumerations.DocType.n2
            'A61 codigo de pais (siempre UY)
            micfe.Encabezado.Receptor.CodPaisRecep = Uruware.LibUcfe.Xml.Enumerations.CodPaisTypeFromString(otr.Rows(0).Item("iso3166"))
            'A62 siempre sería un RUT
            micfe.Encabezado.Receptor.DocRecep = cab.Rows(0).Item("nro_dgi").ToString.Trim
        ElseIf tipoCFE = "151" Or tipoCFE = "152" Then
            'A61 codigo de pais (siempre UY)
            micfe.Encabezado.Receptor.CodPaisRecep = Uruware.LibUcfe.Xml.Enumerations.CodPaisTypeFromString(otr.Rows(0).Item("iso3166"))
            'A62 siempre sería un RUT
            Dim d As Uruware.LibUcfe.Xml.EBoleta
            micfe.Encabezado.Receptor.Receptor_Boleta_Choice = New Uruware.LibUcfe.Xml.Receptor_Boleta_Choice
            micfe.Encabezado.Receptor.Receptor_Boleta_Choice.DocRecep = cab.Rows(0).Item("nro_dgi").ToString.Trim
        End If
        'A63 nombre receptor
        micfe.Encabezado.Receptor.RznSocRecep = cab.Rows(0).Item("nom_tit").ToString.Trim
        'A64 - direccion receptor
        micfe.Encabezado.Receptor.DirRecep = cab.Rows(0).Item("dir_tit").ToString.Trim
        'A65 - ciudad receptor
        micfe.Encabezado.Receptor.CiudadRecep = cab.Rows(0).Item("ciudad_tit").ToString.Trim

        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        '''''''''''''''''''''''''''''''''''''''''''''''''''' DETALLE ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        totMontoNF = 0
        totMontoNG = 0
        totMontoExp = 0
        totMontoTM = 0
        totMontoTB = 0
        totIvaTB = 0
        totIvaTM = 0
        totLineas = lin.Rows.Count

        For i = 0 To lin.Rows.Count - 1
            ' agrego linea de detalle
            If tipoCFE = "151" Or tipoCFE = "152" Then
                micfe.Detalle.Item.Add(New Uruware.LibUcfe.Xml.Item_Det_Boleta())
            Else
                micfe.Detalle.Item.Add(New Uruware.LibUcfe.Xml.Item_Det_Fact())
            End If

            ' B1 - numero de linea
            micfe.Detalle.Item(i).NroLinDet = i + 1
            ' B2 B3 - tabla de codigos de item
            If tipoCFE = "151" Or tipoCFE = "152" Then
                micfe.Detalle.Item(i).CodItem.Add(New Uruware.LibUcfe.Xml.CodItemD)
            Else
                micfe.Detalle.Item(i).CodItem.Add(New Uruware.LibUcfe.Xml.CodItemC)
            End If
            micfe.Detalle.Item(i).CodItem(0).TpoCod = "INT1"
            micfe.Detalle.Item(i).CodItem(0).Cod = lin.Rows(i)("cod_articulo").ToString.Trim
            'msgbox("hola 2")
            ' B4 - indicador de facturacion, se define por la tasa de iva, ya se suman los montos por separado segun la tasa para los totales
            If lin.Rows(i)("porc_iva") = 0 Then     ' iva tasa cero puede ser Exportaciones y asimiladas o Exento en funcion de la cuenta contable de la linea
                If tipoCFE = "151" Or tipoCFE = "152" Then
                    totMontoNG = totMontoNG + lin.Rows(i)("total_linea") 'Math.Round(Convert.ToDouble(fila("total_linea")), 4)
                    micfe.Detalle.Item(i).IndFact = Enumerations.Item_Det_Boleta_IndFact.n15  'item vendido por un contribuyente de IMEBA
                ElseIf cuentasExportAsim.indexof(CInt(lin.Rows(i)("cta_vta_fac"))) >= 0 Then
                    totMontoExp = totMontoExp + lin.Rows(i)("total_linea") 'Math.Round(Convert.ToDouble(fila("total_linea")), 4)
                    micfe.Detalle.Item(i).IndFact = Enumerations.Item_Det_Fact_IndFact.n10  'exportacion y asimiladas
                Else
                    totMontoNG = totMontoNG + lin.Rows(i)("total_linea") 'Math.Round(Convert.ToDouble(fila("total_linea")), 4)
                    micfe.Detalle.Item(i).IndFact = Enumerations.Item_Det_Fact_IndFact.n1  'exento
                End If
            ElseIf lin.Rows(i)("porc_iva") = 10 Then ' tasa minima
                totMontoTM = totMontoTM + lin.Rows(i)("total_linea") 'Math.Round(Convert.ToDouble(fila("total_linea")), 4)
                totIvaTM = totIvaTM + lin.Rows(i)("imp_iva") 'Convert.ToDouble(fila("imp_iva"))
                micfe.Detalle.Item(i).IndFact = Enumerations.Item_Det_Fact_IndFact.n2  'tasa minima
            ElseIf lin.Rows(i)("porc_iva") = 22 Then ' tasa basica
                totMontoTB = totMontoTB + lin.Rows(i)("total_linea") 'Math.Round(Convert.ToDouble(fila("total_linea")), 4)
                totIvaTB = totIvaTB + lin.Rows(i)("imp_iva") 'Convert.ToDouble(fila("imp_iva"))
                micfe.Detalle.Item(i).IndFact = Enumerations.Item_Det_Fact_IndFact.n3  'tasa basica
            Else
                Return 11  ' tasa desconocida
            End If

            ' B7 - nombre del item
            If lin.Rows(i)("nom_det_art").ToString.Trim.Length > 80 Then
                micfe.Detalle.Item(i).NomItem = lin.Rows(i)("nom_det_art").ToString.Trim.Substring(0, 80)
            Else
                micfe.Detalle.Item(i).NomItem = lin.Rows(i)("nom_det_art").ToString.Trim
            End If
            'msgbox("hola 4")


            If cab.Rows(0).Item("formulario") = "VT_NotasCredDesc" Then ' Las notas de crédito por descuento son un caso raro
                ' B9 - cantidad
                'micfe.Detalle.Item(i).Cantidad = 1 - (Math.Round(Convert.ToDouble(lin.Rows(i)("bonif_com")) / 100, 3))
                micfe.Detalle.Item(i).Cantidad = 1
                ' B10 - unidad
                micfe.Detalle.Item(i).UniMed = lin.Rows(i)("cod_unidad")
                ' B11 - precio unitario
                'micfe.Detalle.Item(i).PrecioUnitario = Math.Round(Convert.ToDouble(lin.Rows(i)("precio")), 6)
                micfe.Detalle.Item(i).PrecioUnitario = Math.Round(Convert.ToDouble(lin.Rows(i)("total_linea")), 2, MidpointRounding.AwayFromZero)
                ' B12 y B13 no aplican
            Else
                ' B9 - cantidad
                micfe.Detalle.Item(i).Cantidad = Math.Round(Convert.ToDouble(lin.Rows(i)("cantidad")), 3, MidpointRounding.AwayFromZero)
                ' B10 - unidad
                micfe.Detalle.Item(i).UniMed = lin.Rows(i)("cod_unidad")
                ' B11 - precio unitario
                micfe.Detalle.Item(i).PrecioUnitario = Math.Round(Convert.ToDouble(lin.Rows(i)("precio")), 6, MidpointRounding.AwayFromZero)
                ' B12 - % de descuento
                If Math.Round(Convert.ToDouble(lin.Rows(i)("bonif_com")), 3) <> 0 Then
                    micfe.Detalle.Item(i).DescuentoPct = Math.Round(Convert.ToDouble(lin.Rows(i)("bonif_com")), 3, MidpointRounding.AwayFromZero)
                    ' B13 - importe del descuento
                    micfe.Detalle.Item(i).DescuentoMonto = Math.Round(Convert.ToDouble(lin.Rows(i)("cantidad")) * Convert.ToDouble(lin.Rows(i)("precio")) - Convert.ToDouble(lin.Rows(i)("total_linea")), 2, MidpointRounding.AwayFromZero)
                End If
                ' B24 - monto item
            End If
            micfe.Detalle.Item(i).MontoItem = Math.Round(Convert.ToDouble(lin.Rows(i)("total_linea")), 2, MidpointRounding.AwayFromZero)
        Next

        ' si hay redondeo lo tengo que agregar como una linea en el detalle
        If cab.Rows(0).Item("redondeo") <> 0 Then
            ' esto es para los totales
            totLineas = lin.Rows.Count + 1
            totMontoNF = Convert.ToDouble(cab.Rows(0).Item("redondeo"))
            ' agrego linea de detalle
            micfe.Detalle.Item.Add(New Uruware.LibUcfe.Xml.Item_Det_Fact())
            i = lin.Rows.Count
            ' B1 - numero de linea
            micfe.Detalle.Item(i).NroLinDet = i + 1
            ' B4 - indicador de facturacion, se define por la tasa de iva, ya se suman los montos por separado segun la tasa para los totales
            If Convert.ToDouble(cab.Rows(0).Item("redondeo")) > 0 Then
                micfe.Detalle.Item(i).IndFact = Enumerations.Item_Det_Fact_IndFact.n6 ' no facturable positivo
            Else
                micfe.Detalle.Item(i).IndFact = Enumerations.Item_Det_Fact_IndFact.n7 ' no facturable negativo
            End If
            ' B7 - nombre del item
            micfe.Detalle.Item(i).NomItem = "REDONDEO"
            ' B9 - cantidad
            micfe.Detalle.Item(i).Cantidad = 1
            ' B10 - unidad
            micfe.Detalle.Item(i).UniMed = "N/A"
            ' B11 - precio unitario (siempre positivo)
            micfe.Detalle.Item(i).PrecioUnitario = Math.Round(Math.Abs(totMontoNF), 6, MidpointRounding.AwayFromZero)
            ' B24 - monto item (siempre positivo)
            micfe.Detalle.Item(i).MontoItem = Math.Round(Math.Abs(totMontoNF), 2, MidpointRounding.AwayFromZero)
        End If


        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ''''''''''''''''''''''''''''''''''''''''''''''''' ENCABEZADO ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' volvemos al encabezado para cargar los totales que se calculan a partir de las lineas

        ''''''''''''''''' Totales Cabezal ''''''''''''''''''
        'B110 - tipo moneda
        micfe.Encabezado.Totales.TpoMoneda = Uruware.LibUcfe.Xml.Enumerations.TipMonTypeFromString(cab.Rows(0).Item("id_sopmg_mone"))
        'B111 - tipo de cambio
        If Trim(cab.Rows(0).Item("id_sopmg_mone")) <> "UYU" Then
            micfe.Encabezado.Totales.TpoCambio = Math.Round(Convert.ToDouble(cab.Rows(0).Item("tc_ing")), 3, MidpointRounding.AwayFromZero)
        End If
        'B112 - monto total no gravado
        If Math.Round(totMontoNG, 2) <> 0 Then
            micfe.Encabezado.Totales.MntNoGrv = Math.Round(totMontoNG, 2, MidpointRounding.AwayFromZero)
        End If
        'B113 - monto total exportaciones y asimiladas
        If Math.Round(totMontoExp, 2) <> 0 Then
            micfe.Encabezado.Totales.MntExpoyAsim = Math.Round(totMontoExp, 2, MidpointRounding.AwayFromZero)
        End If
        'B116 - monto total tasa minima
        If Math.Round(totMontoTM, 2) <> 0 Then
            micfe.Encabezado.Totales.MntNetoIvaTasaMin = Math.Round(totMontoTM, 2, MidpointRounding.AwayFromZero)
        End If
        'B117 - monto total tasa basica
        If Math.Round(totMontoTB, 2) <> 0 Then
            micfe.Encabezado.Totales.MntNetoIVATasaBasica = Math.Round(totMontoTB, 2, MidpointRounding.AwayFromZero)
        End If
        ' porcentajes de iva
        If tipoCFE <> "151" And tipoCFE <> "152" Then
            'B119 - tasa minima
            micfe.Encabezado.Totales.IVATasaMin = Uruware.LibUcfe.Xml.Enumerations.TasaIVAType.n10FullStop000
            'B120 - tasa basica
            micfe.Encabezado.Totales.IVATasaBasica = Uruware.LibUcfe.Xml.Enumerations.TasaIVAType.n22FullStop000
        End If
        'B121 - total iva tasa minima
        If Math.Round(totIvaTM, 2) <> 0 Then
            micfe.Encabezado.Totales.MntIVATasaMin = Math.Round(totIvaTM, 2, MidpointRounding.AwayFromZero)
        End If
        'B122 - total iva tasa basica
        If Math.Round(totIvaTB, 2) <> 0 Then
            micfe.Encabezado.Totales.MntIVATasaBasica = Math.Round(totIvaTB, 2, MidpointRounding.AwayFromZero)
        End If
        '-------------------
        'B124 - total monto total
        micfe.Encabezado.Totales.MntTotal = Math.Round(totMontoNG, 2, MidpointRounding.AwayFromZero) + Math.Round(totMontoExp, 2, MidpointRounding.AwayFromZero) + Math.Round(totMontoTM, 2, MidpointRounding.AwayFromZero) + Math.Round(totMontoTB, 2, MidpointRounding.AwayFromZero) + Math.Round(totIvaTM, 2, MidpointRounding.AwayFromZero) + Math.Round(totIvaTB, 2, MidpointRounding.AwayFromZero)
        'B126 - lineas
        micfe.Encabezado.Totales.CantLinDet = totLineas
        'B129 - monto no facturable (redondeo con signo y todo) (admite negativos)
        If Math.Round(totMontoNF, 2) <> 0 Then
            micfe.Encabezado.Totales.MontoNF = Math.Round(totMontoNF, 2, MidpointRounding.AwayFromZero)
        End If
        'B130 - monto total a pagar
        micfe.Encabezado.Totales.MntPagar = micfe.Encabezado.Totales.MntTotal + Math.Round(totMontoNF, 2, MidpointRounding.AwayFromZero)


        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ''''''''''''''''''''''''''''''''''''''''''''''''' REFERENCIAS '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        If tipoCFE = "102" Or tipoCFE = "112" Then ' solo las NC tienen referencias
            ' creo el objeto Referencias
            micfe.Referencia = New Uruware.LibUcfe.Xml.Referencia()
            ' le agrego un item
            micfe.Referencia.ReferenciaA.Add(New Uruware.LibUcfe.Xml.ReferenciaA())

            ' F1 - numero de linea de referencia
            micfe.Referencia.ReferenciaA.Item(0).NroLinRef = 1
            x.Text = "SELECT fec_doc from cpt_facturas where cod_doc='" & Trim(cab.Rows(0).Item("cod_doc_fac").ToString) & "' and serie_cfe = '" & Trim(cab.Rows(0).Item("serie_factura").ToString) & "' and nro_doc=" & Trim(cab.Rows(0).Item("nro_factura").ToString)
            otr2 = SQL.SelDataTable(x, sqlParams)
            If Trim(cab.Rows(0).Item("cod_doc_fac")) = "fact" And otr2.Rows.Count = 1 Then
                micfe.Referencia.ReferenciaA.Item(0).TpoDocRef = Enumerations.CFEType.n111
                micfe.Referencia.ReferenciaA.Item(0).Serie = cab.Rows(0).Item("serie_factura").ToString.Trim
                micfe.Referencia.ReferenciaA.Item(0).NroCFERef = CInt(cab.Rows(0).Item("nro_factura"))
                micfe.Referencia.ReferenciaA.Item(0).RazonRef = "Nota de crédito eFactura"
                fechaAux = otr2.Rows(0).Item("fec_doc")
                micfe.Referencia.ReferenciaA.Item(0).FechaCFEref = New LiquidTechnologies.Runtime.Net40.XmlDateTime(fechaAux)
            ElseIf Trim(cab.Rows(0).Item("cod_doc_fac")) = "tick" And otr2.Rows.Count = 1 Then
                micfe.Referencia.ReferenciaA.Item(0).TpoDocRef = Enumerations.CFEType.n101
                micfe.Referencia.ReferenciaA.Item(0).Serie = cab.Rows(0).Item("serie_factura").ToString.Trim
                micfe.Referencia.ReferenciaA.Item(0).NroCFERef = CInt(cab.Rows(0).Item("nro_factura"))
                micfe.Referencia.ReferenciaA.Item(0).RazonRef = "Nota de crédito eTicket"
                fechaAux = otr2.Rows(0).Item("fec_doc")
                micfe.Referencia.ReferenciaA.Item(0).FechaCFEref = New LiquidTechnologies.Runtime.Net40.XmlDateTime(fechaAux)
            Else
                micfe.Referencia.ReferenciaA.Item(0).IndGlobal = Enumerations.ReferenciaA_IndGlobal.n1 'referencia global
                If Trim(cab.Rows(0).Item("nro_factura").ToString) = "0" Then
                    micfe.Referencia.ReferenciaA.Item(0).RazonRef = "Referencia a documento no determinado"
                Else
                    micfe.Referencia.ReferenciaA.Item(0).RazonRef = "Referencia a documento no electrónico: código: " & cab.Rows(0).Item("cod_doc_fac").ToString & " serie: " & cab.Rows(0).Item("serie_factura").ToString & " número: " & cab.Rows(0).Item("nro_factura").ToString
                End If
            End If

        ElseIf tipoCFE = "152" Then ' solo las NC tienen referencias
            ' creo el objeto Referencias
            micfe.Referencia = New Uruware.LibUcfe.Xml.Referencia()
            ' le agrego un item
            micfe.Referencia.ReferenciaA.Add(New Uruware.LibUcfe.Xml.ReferenciaA())
            ' F1 - numero de linea de referencia
            micfe.Referencia.ReferenciaA.Item(0).NroLinRef = 1
            ' busco la ultima boleta de entrada del mismo titular (la fecha es para que sea posterior al inicio de las boletas electrònicas)
            '----- !!!!!!!!!! XXXXX aca debe decir SERIE_CFE pero falta agregar el campo a la tabla XXXXXXXXXXXXXXXXXXX
            '----- !!!!!!!!!! XXXXX aca debe decir SERIE_CFE pero falta agregar el campo a la tabla XXXXXXXXXXXXXXXXXXX
            '----- !!!!!!!!!! XXXXX aca debe decir SERIE_CFE pero falta agregar el campo a la tabla XXXXXXXXXXXXXXXXXXX
            '----- !!!!!!!!!! XXXXX aca debe decir SERIE_CFE pero falta agregar el campo a la tabla XXXXXXXXXXXXXXXXXXX
            x.Text = "select nro_doc, fec_doc, serie_doc from cpt_nc_prod_cab where fec_doc>='2018-06-28 00:00:00' and nro_trans = (SELECT max(CAB.nro_trans) from cpt_nc_prod_cab CAB, cpf_movcontdir CONT where CAB.nro_trans = CONT.nro_trans and CONT.signo = 1 and CAB.cod_tit=" & cab.Rows(0).Item("cod_tit").ToString.Trim & " and CAB.nro_trans<" & cab.Rows(0).Item("nro_trans").ToString.Trim & ")"
            '----- !!!!!!!!!! XXXXX aca debe decir SERIE_CFE pero falta agregar el campo a la tabla XXXXXXXXXXXXXXXXXXX
            '----- !!!!!!!!!! XXXXX aca debe decir SERIE_CFE pero falta agregar el campo a la tabla XXXXXXXXXXXXXXXXXXX
            '----- !!!!!!!!!! XXXXX aca debe decir SERIE_CFE pero falta agregar el campo a la tabla XXXXXXXXXXXXXXXXXXX
            '----- !!!!!!!!!! XXXXX aca debe decir SERIE_CFE pero falta agregar el campo a la tabla XXXXXXXXXXXXXXXXXXX
            '----- !!!!!!!!!! XXXXX aca debe decir SERIE_CFE pero falta agregar el campo a la tabla XXXXXXXXXXXXXXXXXXX
            otr2 = SQL.SelDataTable(x, sqlParams)
            If otr2.Rows.Count = 0 Then
                ' no tengo referencia
                micfe.Referencia.ReferenciaA.Item(0).IndGlobal = Enumerations.ReferenciaA_IndGlobal.n1 'referencia global
                micfe.Referencia.ReferenciaA.Item(0).RazonRef = "Referencia a documento no determinado"
            Else
                micfe.Referencia.ReferenciaA.Item(0).TpoDocRef = Enumerations.CFEType.n151
                micfe.Referencia.ReferenciaA.Item(0).Serie = otr.Rows(0).Item("serie_cfe").ToString.Trim
                micfe.Referencia.ReferenciaA.Item(0).NroCFERef = CInt(cab.Rows(0).Item("nro_doc"))
                micfe.Referencia.ReferenciaA.Item(0).RazonRef = "e-Boleta de entrada"
                fechaAux = otr2.Rows(0).Item("fec_doc")
                micfe.Referencia.ReferenciaA.Item(0).FechaCFEref = New LiquidTechnologies.Runtime.Net40.XmlDateTime(fechaAux)
            End If


        End If

        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        '''''''''''''''''''''''''''''''''''''''''''''''''''' ADENDA '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        Me.adenda = "Adenda\Recibí conforme ___________________\"

        Return 0

    End Function

    Public Function ObtenerDatosExport(ByVal _transaccion As System.Data.IDbTransaction, ByVal tipoCFE As String, ByVal trans As String) As Integer
        Dim cab As System.Data.DataTable
        Dim lin As System.Data.DataTable
        Dim otr As System.Data.DataTable
        Dim i, totLineas As Integer
        Dim totMontoExp As Double
        Dim totMontoNF As Double
        Dim fpagoContado As Integer = 101  ' CODIGO DE FORMA DE PAGO QUE REPRESENTA EL PAGO CONTADO
        Dim sqlCab, sqlLin As String
        Dim result As Boolean = False
        Dim sqlParams As New SQLParameters(_transaccion)
        Dim x As New SQLCommand
        Dim fechaAux As Date
        Dim micfe

        x.Text = "select valor_param_char from ct_paramxemp where cod_emp = 'casa' and cod_parametro = 'codcomercio'"
        otr = SQL.SelDataTable(x, sqlParams)
        If otr.Rows.Count <> 1 Then Return 3 ' no se encontró parametro codcomercio
        Me.codComercio = otr.Rows(0).Item("valor_param_char")
        ' Para Testing
        'Me.codComercio = "Casaro-1"

        x.Text = "select valor_param_char from ct_paramxemp where cod_emp = 'casa' and cod_parametro = 'codterminal'"
        otr = SQL.SelDataTable(x, sqlParams)
        If otr.Rows.Count <> 1 Then Return 4 ' no se encontró parametro codterminal
        Me.codTerminal = otr.Rows(0).Item("valor_param_char")
        ' Para Testing
        'Me.codTerminal = "FC-1"

        If tipoCFE = "121" Then   ' factura export
            sqlCab = "select CAB.fec_fiscal, CAB.cod_fpago, CAB.claus_venta, CAB.modal_venta, CAB.nro_tipotrans, CAB.cod_sucursal, CAB.nom_pais, "
            sqlCab += " CAB.nom_tit, CAB.dir_tit, CAB.ciudad_tit, CAB.nom_provincia, MON.id_sopmg_mone, CAB.tc_ing, CAB.redondeo, CAB.es_fiscal, CAB.imp_flete_intnal, CAB.imp_seguro, EX2.adenda_export, 10 indicador_fact "
            sqlCab += " FROM cpt_facturas_exp CAB left join cpt_facturas_ex2 EX2 on CAB.nro_trans = EX2.nro_trans, ct_monedas MON "
            sqlCab += " WHERE  CAB.cod_moneda = MON.cod_moneda and CAB.nro_trans = " & trans
            x.Text = sqlCab
            cab = SQL.SelDataTable(x, sqlParams)
            If cab.Rows.Count = 0 Then Return 5 ' no se encontró el cabezal de la factura
            ' --- Específico de facturas de exportación - reviso si es fiscal, en caso contrario retorno sin hacer nada
            If cab.Rows(0).Item("es_fiscal").ToString.Trim.ToUpper <> "S" Then Return 12
            ' ------------------
            sqlLin = "SELECT LIN.linea, ART.nom_articulo, LIN.cantidad, LIN.cod_uni_exp, LIN.precio, LIN.cod_articulo FROM cpt_factexp_lin LIN, ct_articulos ART WHERE lin.cod_articulo = ART.cod_articulo And lin.nro_trans = " & trans
            x.Text = sqlLin
            lin = SQL.SelDataTable(x, sqlParams)
            If lin.Rows.Count = 0 Then Return 6 ' no se encontraron las lineas de la fatura
            Me.emailReceptor = "" ' por el momento no se mandan las facturas de exportacion
        ElseIf tipoCFE = "122" Then   ' NC export
            sqlCab = "select CAB.fec_doc fec_fiscal, CAB.cod_fpago, nvl(F.claus_venta,'N/A') claus_venta, nvl(F.modal_venta,1) modal_venta,"
            sqlCab += " nvl(F.nro_tipotrans,8) nro_tipotrans, CAB.cod_sucursal, nvl(F.nom_pais,'pais') nom_pais, nvl(F.nom_tit,'nombre') nom_tit,"
            sqlCab += " nvl(F.dir_tit,'direccion') dir_tit, nvl(F.ciudad_tit,'ciudad') ciudad_tit, nvl(F.nom_provincia,'provincia') nom_provincia,"
            sqlCab += " MON.id_sopmg_mone, CAB.tc_ing, 0 redondeo, F.es_fiscal, CAB.imp_flete_intnal, CAB.imp_seguro, '' adenda_export,"
            sqlCab += " F.nro_cfe nro_cfe_ref, F.serie_cfe serie_cfe_ref, F.fec_fiscal fec_fiscal_ref, F.cod_doc cod_doc_ref, CAB.nro_factexp, "
            sqlCab += " 10 indicador_fact"
            sqlCab += " FROM cpt_notascre_exp CAB, cpt_facturas_exp F, ct_monedas MON "
            sqlCab += " WHERE CAB.cod_moneda = MON.cod_moneda and CAB.nro_factexp = F.nro_factexp and CAB.nro_trans = " & trans
            x.Text = sqlCab
            cab = SQL.SelDataTable(x, sqlParams)

            If cab.Rows.Count <> 1 Then Return 5 ' no se encontró el cabezal de la NC ( o hay 2 facturas exp con el mismo numero comercial
            ' --- Específico de NC de exportación - reviso si es fiscal la factura asociada a la NC, en caso contrario retorno error
            If cab.Rows(0).Item("es_fiscal").ToString.Trim.ToUpper <> "S" Then Return 13
            ' ------------------
            sqlLin = "SELECT LIN.linea, ART.nom_articulo, LIN.cantidad, LIN.cod_uni_ven cod_uni_exp, LIN.precio, LIN.cod_articulo FROM cpt_notcrexp_lin LIN, ct_articulos ART WHERE lin.cod_articulo = ART.cod_articulo And lin.nro_trans = " & trans
            x.Text = sqlLin
            lin = SQL.SelDataTable(x, sqlParams)
            If lin.Rows.Count = 0 Then Return 6 ' no se encontraron las lineas de la NC
            Me.emailReceptor = "" ' por el momento no se mandan las NC de exportacion
        ElseIf tipoCFE = "124" Or tipoCFE = "224" Then   ' remito de exportación y contingencia   
            sqlCab = "select fec_doc fec_fiscal, claus_venta, modal_venta, nro_tipotrans, cod_sucursal, nom_pais, nom_tit, dir_entrega dir_tit, "
            sqlCab += " localidad ciudad_tit, nom_provincia, id_sopmg_mone, nvl(tc_ing,1) tc_ing, 0 redondeo, 0 imp_flete_intnal, 0 imp_seguro, "
            sqlCab += " '' adenda_export, nvl(nro_cfe_ref,0) nro_cfe_ref, serie_cfe_ref, fec_fiscal fec_fiscal_ref, cod_doc_ref, nro_factexp, "
            sqlCab += " indicador_fact, serie_cfe, nro_cfe, "
            sqlCab += " (select nvl(x.mail,'') from ct_mailefact x where x.cod_deposito = cpt_notas_ent.cod_deposito) emailReceptor "
            sqlCab += " FROM cpt_notas_ent WHERE nro_trans = " & trans
            x.Text = sqlCab
            cab = SQL.SelDataTable(x, sqlParams)

            If cab.Rows.Count <> 1 Then Return 5 ' no se encontró el cabezal del remito ( o hay 2 facturas exp con el mismo numero comercial
            sqlLin = "SELECT linea, nom_det_art nom_articulo, cantidad, cod_uni_ven cod_uni_exp, precio, cod_articulo FROM cpt_notasent_lin WHERE nro_trans = " & trans
            x.Text = sqlLin
            lin = SQL.SelDataTable(x, sqlParams)
            If lin.Rows.Count = 0 Then Return 6 ' no se encontraron las lineas del remito

            ' se mandan los remitos de exportacion por mail al si tienen un deposito definido con mail
            Me.emailReceptor = cab.Rows(0).Item("emailReceptor").ToString.Trim
            Me.emailReceptor = Strings.Replace(Me.emailReceptor, "#", "@")
            Me.emailReceptor = Strings.Replace(Me.emailReceptor, " ", "")
        End If

        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ''''''''''''''''''''''''''''''''''''''''''''''''' ENCABEZADO ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        ' creo el objeto correspondiente
        If tipoCFE = "121" Then ' Factura exportacion eFaxturaExportacion
            Me.cfe.EFact_Exp = New Uruware.LibUcfe.Xml.EFact_Exp()
            Me.cfe.EFact_Exp.Encabezado.IdDoc.TipoCFE = Enumerations.IdDoc_Fact_Exp_TipoCFE.n121
            micfe = Me.cfe.EFact_Exp
        ElseIf tipoCFE = "122" Then ' NC exportacion eFacturaExportacion
            Me.cfe.EFact_Exp = New Uruware.LibUcfe.Xml.EFact_Exp()
            Me.cfe.EFact_Exp.Encabezado.IdDoc.TipoCFE = Enumerations.IdDoc_Fact_Exp_TipoCFE.n122
            micfe = Me.cfe.EFact_Exp
        ElseIf tipoCFE = "124" Then ' remitode exportacion se utiliza eRemitoExportacion
            Me.cfe.ERem_Exp = New Uruware.LibUcfe.Xml.ERem_Exp()
            Me.cfe.ERem_Exp.Encabezado.IdDoc.TipoCFE = Enumerations.IdDoc_Rem_Exp_TipoCFE.n124
            micfe = Me.cfe.ERem_Exp
        ElseIf tipoCFE = "224" Then ' NC crédito se utiliza eFactura
            Me.cfe.ERem_Exp = New Uruware.LibUcfe.Xml.ERem_Exp()
            Me.cfe.ERem_Exp.Encabezado.IdDoc.TipoCFE = Enumerations.IdDoc_Rem_Exp_TipoCFE.n224
            micfe = Me.cfe.ERem_Exp
        End If
        'micfe se utiliza para poder referenciar al comprobante que sea

        '''''''' Identificacion del comprobante '''''''
        If tipoCFE = "224" Then   ' para los CFE de contingencia debo indicar serie y numero !!!!! CONTROLAR QUE NO VAYAN A ESTAR EN BLANCO
            micfe.Encabezado.IdDoc.Serie = cab.Rows(0).Item("serie_cfe").ToString.Trim
            micfe.Encabezado.IdDoc.Nro = CInt(cab.Rows(0).Item("nro_cfe"))
        End If
        fechaAux = cab.Rows(0).Item("fec_fiscal")
        micfe.Encabezado.IdDoc.FchEmis = New LiquidTechnologies.Runtime.Net40.XmlDateTime(fechaAux)
        If tipoCFE = "124" Or tipoCFE = "224" Then ' indicador de traslado de bienes, obligatorio para remitos, 1=Venta, 2=Traslados internos
            micfe.Encabezado.IdDoc.TipoTraslado = Uruware.LibUcfe.Xml.Enumerations.TipoTrasladoType.n1
        End If

        If tipoCFE = "121" Or tipoCFE = "122" Then   ' para Factura y NC debo indicar la forma de pago
            If CInt(cab.Rows(0).Item("cod_fpago")) = fpagoContado Then
                micfe.Encabezado.IdDoc.FmaPago = Uruware.LibUcfe.Xml.Enumerations.IdDoc_Fact_FmaPago.n1
            Else
                micfe.Encabezado.IdDoc.FmaPago = Uruware.LibUcfe.Xml.Enumerations.IdDoc_Fact_FmaPago.n2
            End If
        End If
        micfe.Encabezado.IdDoc.ClauVenta = cab.Rows(0).Item("claus_venta").ToString.Trim
        micfe.Encabezado.IdDoc.ModVenta = Uruware.LibUcfe.Xml.Enumerations.ModVentaTypeFromString(cab.Rows(0).Item("modal_venta").ToString.Trim)
        micfe.Encabezado.IdDoc.ViaTransp = Uruware.LibUcfe.Xml.Enumerations.ViaTranspTypeFromString(cab.Rows(0).Item("nro_tipotrans").ToString.Trim)


        '''''''''''''''' Emisor '''''''''''''''''''''''
        x.Text = "select valor_param_char from ct_paramxemp where cod_emp = 'casa' and cod_parametro = 'rut'"
        otr = SQL.SelDataTable(x, sqlParams)
        If otr.Rows.Count <> 1 Then Return 1 ' no se encontró parametro rut
        micfe.Encabezado.Emisor.RUCEmisor = Val(otr.Rows(0).Item("valor_param_char"))

        x.Text = "select valor_param_char from ct_paramxemp where cod_emp = 'casa' and cod_parametro = 'emisor'"
        otr = SQL.SelDataTable(x, sqlParams)
        If otr.Rows.Count <> 1 Then Return 2 ' no se encontró parametro emisor
        micfe.Encabezado.Emisor.RznSoc = otr.Rows(0).Item("valor_param_char").ToString.Trim

        x.Text = "select cod_sucursal, nom_sucursal, dir_planta, ciudad_tit, nom_depto_uy from ct_sucursales where cod_sucursal=" & cab.Rows(0).Item("cod_sucursal").ToString
        otr = SQL.SelDataTable(x, sqlParams)
        If otr.Rows.Count = 0 Then Return 7 ' no se encontraró la sucursal indicada en la factura
        micfe.Encabezado.Emisor.EmiSucursal = otr.Rows(0).Item("nom_sucursal")
        micfe.Encabezado.Emisor.CdgDGISucur = CInt(otr.Rows(0).Item("cod_sucursal"))
        micfe.Encabezado.Emisor.DomFiscal = otr.Rows(0).Item("dir_planta")
        micfe.Encabezado.Emisor.Ciudad = otr.Rows(0).Item("ciudad_tit")
        micfe.Encabezado.Emisor.Departamento = otr.Rows(0).Item("nom_depto_uy")

        '''''''''''''''''' Receptor '''''''''''''''''''
        If tipoCFE = "124" Or tipoCFE = "224" Then ' remitos + - 
            micfe.Encabezado.Receptor = New Uruware.LibUcfe.Xml.Receptor_Rem_Exp()
        ElseIf tipoCFE = "121" Or tipoCFE = "122" Then ' facturas y nc
            micfe.Encabezado.Receptor = New Uruware.LibUcfe.Xml.Receptor_Fact_Exp()
        End If
        micfe.Encabezado.Receptor.RznSocRecep = cab.Rows(0).Item("nom_tit").ToString.Substring(0, Math.Min(cab.Rows(0).Item("nom_tit").ToString.Trim.Length, 150))
        micfe.Encabezado.Receptor.DirRecep = cab.Rows(0).Item("dir_tit").ToString.Substring(0, Math.Min(cab.Rows(0).Item("dir_tit").ToString.Trim.Length, 70))
        micfe.Encabezado.Receptor.CiudadRecep = cab.Rows(0).Item("ciudad_tit").ToString.Substring(0, Math.Min(cab.Rows(0).Item("ciudad_tit").ToString.Trim.Length, 30))
        micfe.Encabezado.Receptor.DeptoRecep = cab.Rows(0).Item("nom_provincia").ToString.Substring(0, Math.Min(cab.Rows(0).Item("nom_provincia").ToString.Trim.Length, 30))
        micfe.Encabezado.Receptor.PaisRecep = cab.Rows(0).Item("nom_pais").ToString.Substring(0, Math.Min(cab.Rows(0).Item("nom_pais").ToString.Trim.Length, 30))


        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        '''''''''''''''''''''''''''''''''''''''''''''''''''' DETALLE ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        '''''''''''''''Defino Detalle y Totales linea'''''''''''''''''''
        totMontoExp = 0
        totMontoNF = 0
        totLineas = lin.Rows.Count

        For i = 0 To lin.Rows.Count - 1
            ' agrego linea de detalle
            If tipoCFE = "121" Or tipoCFE = "122" Then ' item de detalle para factura exp o NC exp
                micfe.Detalle.Item.Add(New Uruware.LibUcfe.Xml.Item_Det_Fact_Exp())
            ElseIf tipoCFE = "124" Or tipoCFE = "224" Then ' item de detalle para remito exp
                micfe.Detalle.Item.Add(New Uruware.LibUcfe.Xml.Item_Rem_Exp())
            End If
            ' B1 - numero de linea
            micfe.Detalle.Item(i).NroLinDet = i + 1
            If tipoCFE = "121" Or tipoCFE = "122" Then   ' para Factura y NC indicador = 10 'export y asimiladas'
                micfe.Detalle.Item(i).IndFact = Enumerations.Item_Det_Fact_Exp_IndFact.n10
            ElseIf (tipoCFE = "124" Or tipoCFE = "224") And cab.Rows(0).Item("indicador_fact").ToString.Trim = "8" Then
                ' para los remitos, solo hay que indicar si es una ajuste indice=8, si es un remito comun el indice queda en blanco
                micfe.Detalle.Item(i).IndFact = Enumerations.Item_Rem_Exp_IndFact.n8
            End If
            micfe.Detalle.Item(i).NomItem = lin.Rows(i)("nom_articulo").ToString.Trim
            micfe.Detalle.Item(i).Cantidad = Math.Round(Convert.ToDouble(lin.Rows(i)("cantidad")), 3, MidpointRounding.AwayFromZero)
            micfe.Detalle.Item(i).UniMed = lin.Rows(i)("cod_uni_exp").ToString.Trim
            micfe.Detalle.Item(i).PrecioUnitario = Math.Round(Convert.ToDouble(lin.Rows(i)("precio")), 6, MidpointRounding.AwayFromZero)
            micfe.Detalle.Item(i).MontoItem = Math.Round(micfe.Detalle.Item(i).Cantidad * micfe.Detalle.Item(i).PrecioUnitario, 2, MidpointRounding.AwayFromZero)
            If (tipoCFE = "124" Or tipoCFE = "224") And cab.Rows(0).Item("indicador_fact").ToString.Trim = "8" Then
                ' para los remitos con indice 8 los importes totales deben ser negativos, no asi los de las lineas
                totMontoExp = totMontoExp + micfe.Detalle.Item(i).Cantidad * micfe.Detalle.Item(i).PrecioUnitario * -1
            Else
                totMontoExp = totMontoExp + micfe.Detalle.Item(i).Cantidad * micfe.Detalle.Item(i).PrecioUnitario
            End If
        Next

        ' si hay flete internacional lo tengo que agregar como una linea en el detalle
        If Convert.ToDouble(cab.Rows(0).Item("imp_flete_intnal")) > 0 Then
            ' esto es para los totales
            totMontoExp = totMontoExp + Convert.ToDouble(cab.Rows(0).Item("imp_flete_intnal"))
            ' agrego linea de detalle
            If tipoCFE = "121" Or tipoCFE = "122" Then ' item de detalle para factura exp o NC exp
                micfe.Detalle.Item.Add(New Uruware.LibUcfe.Xml.Item_Det_Fact_Exp())
            ElseIf tipoCFE = "124" Or tipoCFE = "224" Then ' item de detalle para remito exp
                micfe.Detalle.Item.Add(New Uruware.LibUcfe.Xml.Item_Rem_Exp())
            End If
            ' B1 - numero de linea
            micfe.Detalle.Item(totLineas).NroLinDet = totLineas + 1
            ' B4 - indicador de facturacion
            micfe.Detalle.Item(totLineas).IndFact = Enumerations.Item_Det_Fact_Exp_IndFact.n10 ' no facturable positivo
            ' B7 - nombre del item
            micfe.Detalle.Item(totLineas).NomItem = "FLETE"
            ' B9 - cantidad
            micfe.Detalle.Item(totLineas).Cantidad = 1
            ' B10 - unidad
            micfe.Detalle.Item(totLineas).UniMed = "N/A"
            ' B11 - precio unitario (siempre positivo)
            micfe.Detalle.Item(totLineas).PrecioUnitario = Math.Round(Convert.ToDouble(cab.Rows(0).Item("imp_flete_intnal")), 6, MidpointRounding.AwayFromZero)
            ' B24 - monto item (siempre positivo)
            micfe.Detalle.Item(totLineas).MontoItem = Math.Round(Convert.ToDouble(cab.Rows(0).Item("imp_flete_intnal")), 2, MidpointRounding.AwayFromZero)
            totLineas = totLineas + 1
        End If

        ' si hay seguro lo tengo que agregar como una linea en el detalle
        If Convert.ToDouble(cab.Rows(0).Item("imp_seguro")) > 0 Then
            ' esto es para los totales
            totMontoExp = totMontoExp + Convert.ToDouble(cab.Rows(0).Item("imp_seguro"))
            ' agrego linea de detalle
            If tipoCFE = "121" Or tipoCFE = "122" Then ' item de detalle para factura exp o NC exp
                micfe.Detalle.Item.Add(New Uruware.LibUcfe.Xml.Item_Det_Fact_Exp())
            ElseIf tipoCFE = "124" Or tipoCFE = "224" Then ' item de detalle para remito exp
                micfe.Detalle.Item.Add(New Uruware.LibUcfe.Xml.Item_Rem_Exp())
            End If
            ' B1 - numero de linea
            micfe.Detalle.Item(totLineas).NroLinDet = totLineas + 1
            ' B4 - indicador de facturacion
            micfe.Detalle.Item(totLineas).IndFact = Enumerations.Item_Det_Fact_Exp_IndFact.n10 ' no facturable positivo
            ' B7 - nombre del item
            micfe.Detalle.Item(totLineas).NomItem = "SEGURO"
            ' B9 - cantidad
            micfe.Detalle.Item(totLineas).Cantidad = 1
            ' B10 - unidad
            micfe.Detalle.Item(totLineas).UniMed = "N/A"
            ' B11 - precio unitario (siempre positivo)
            micfe.Detalle.Item(totLineas).PrecioUnitario = Math.Round(Convert.ToDouble(cab.Rows(0).Item("imp_seguro")), 6, MidpointRounding.AwayFromZero)
            ' B24 - monto item (siempre positivo)
            micfe.Detalle.Item(totLineas).MontoItem = Math.Round(Convert.ToDouble(cab.Rows(0).Item("imp_seguro")), 2, MidpointRounding.AwayFromZero)
            totLineas = totLineas + 1
        End If

        ' si hay redondeo lo tengo que agregar como una linea en el detalle
        If Convert.ToDouble(cab.Rows(0).Item("redondeo")) <> 0 Then
            ' esto es para los totales
            totMontoNF = Convert.ToDouble(cab.Rows(0).Item("redondeo"))
            ' agrego linea de detalle
            If tipoCFE = "121" Or tipoCFE = "122" Then ' item de detalle para factura exp o NC exp
                micfe.Detalle.Item.Add(New Uruware.LibUcfe.Xml.Item_Det_Fact_Exp())
            ElseIf tipoCFE = "124" Or tipoCFE = "224" Then ' item de detalle para remito exp
                micfe.Detalle.Item.Add(New Uruware.LibUcfe.Xml.Item_Rem_Exp())
            End If
            ' B1 - numero de linea
            micfe.Detalle.Item(totLineas).NroLinDet = totLineas + 1
            ' B4 - indicador de facturacion
            If Convert.ToDouble(cab.Rows(0).Item("redondeo")) > 0 Then
                micfe.Detalle.Item(totLineas).IndFact = Enumerations.Item_Det_Fact_Exp_IndFact.n6 ' no facturable positivo
            Else
                micfe.Detalle.Item(totLineas).IndFact = Enumerations.Item_Det_Fact_Exp_IndFact.n7 ' no facturable negativo
            End If
            ' B7 - nombre del item
            micfe.Detalle.Item(totLineas).NomItem = "REDONDEO"
            ' B9 - cantidad
            micfe.Detalle.Item(totLineas).Cantidad = 1
            ' B10 - unidad
            micfe.Detalle.Item(totLineas).UniMed = "N/A"
            ' B11 - precio unitario (siempre positivo)
            micfe.Detalle.Item(totLineas).PrecioUnitario = Math.Round(Math.Abs(totMontoNF), 6, MidpointRounding.AwayFromZero)
            ' B24 - monto item (siempre positivo)
            micfe.Detalle.Item(totLineas).MontoItem = Math.Round(Math.Abs(totMontoNF), 2, MidpointRounding.AwayFromZero)
            totLineas = totLineas + 1
        End If

        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ''''''''''''''''''''''''''''''''''''''''''''''''' ENCABEZADO ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' volvemos al encabezado para cargar los totales que se calculan a partir de las lineas

        ''''''''''''''''' Totales Cabezal ''''''''''''''''''
        'B110 - tipo moneda
        micfe.Encabezado.Totales.TpoMoneda = Uruware.LibUcfe.Xml.Enumerations.TipMonTypeFromString(cab.Rows(0).Item("id_sopmg_mone"))
        'B111 - tipo de cambio
        If Trim(cab.Rows(0).Item("id_sopmg_mone")) <> "UYU" Then
            micfe.Encabezado.Totales.TpoCambio = Math.Round(Convert.ToDouble(cab.Rows(0).Item("tc_ing")), 3, MidpointRounding.AwayFromZero)
        End If
        'B113 - monto total exportaciones y asimiladas
        If Math.Round(totMontoExp, 2, MidpointRounding.AwayFromZero) <> 0 Then
            micfe.Encabezado.Totales.MntExpoyAsim = Math.Round(totMontoExp, 2, MidpointRounding.AwayFromZero)
        End If

        'B124 - total monto total (admite negativos para remitos de exportacion con indice=8)
        micfe.Encabezado.Totales.MntTotal = Math.Round(totMontoExp, 2, MidpointRounding.AwayFromZero)
        'B126 - lineas
        micfe.Encabezado.Totales.CantLinDet = totLineas
        'B129 - monto no facturable (redondeo con signo y todo) (admite negativos) (básicamente son los redondeos)
        If Math.Round(totMontoNF, 2, MidpointRounding.AwayFromZero) <> 0 Then
            micfe.Encabezado.Totales.MontoNF = Math.Round(totMontoNF, 2, MidpointRounding.AwayFromZero)
        End If
        'B130 - monto total a pagar
        micfe.Encabezado.Totales.MntPagar = micfe.Encabezado.Totales.MntTotal + Math.Round(totMontoNF, 2, MidpointRounding.AwayFromZero)



        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ''''''''''''''''''''''''''''''''''''''''''''''''' REFERENCIAS '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        If tipoCFE = "122" Or tipoCFE = "124" Or tipoCFE = "224" Then ' NC , remito y remito contingencia
            ' creo el objeto Referencias
            micfe.Referencia = New Uruware.LibUcfe.Xml.Referencia()
            ' le agrego un item
            micfe.Referencia.ReferenciaA.Add(New Uruware.LibUcfe.Xml.ReferenciaA())

            ' F1 - numero de linea de referencia
            micfe.Referencia.ReferenciaA.Item(0).NroLinRef = 1
            If cab.Rows(0).Item("cod_doc_ref").ToString.Trim = "fex" Then
                If Trim(cab.Rows(0).Item("nro_cfe_ref").ToString) = "0" Then
                    ' esto pasa en los remitos, que se hacen antes de que la factura sea fiscal,
                    ' entonces la factura no tiene numero de CFE, la mandamos como referencia global y le ponemos el numero comercial
                    micfe.Referencia.ReferenciaA.Item(0).IndGlobal = Enumerations.ReferenciaA_IndGlobal.n1 'referencia global
                    micfe.Referencia.ReferenciaA.Item(0).RazonRef = "Referencia a documento no electrónico: Factura de exportación número: " & cab.Rows(0).Item("nro_factexp").ToString.Trim
                Else
                    micfe.Referencia.ReferenciaA.Item(0).TpoDocRef = Enumerations.CFEType.n121
                    micfe.Referencia.ReferenciaA.Item(0).Serie = cab.Rows(0).Item("serie_cfe_ref").ToString.Trim
                    micfe.Referencia.ReferenciaA.Item(0).NroCFERef = CInt(cab.Rows(0).Item("nro_cfe_ref"))
                    micfe.Referencia.ReferenciaA.Item(0).RazonRef = "Referencia a eFactura de exportación"
                    fechaAux = cab.Rows(0).Item("fec_fiscal_ref")
                    micfe.Referencia.ReferenciaA.Item(0).FechaCFEref = New LiquidTechnologies.Runtime.Net40.XmlDateTime(fechaAux)
                End If
            ElseIf cab.Rows(0).Item("cod_doc_ref").ToString.Trim = "rex" Then ' referencia a un eRemito de exportacion
                micfe.Referencia.ReferenciaA.Item(0).TpoDocRef = Enumerations.CFEType.n124
                micfe.Referencia.ReferenciaA.Item(0).Serie = cab.Rows(0).Item("serie_cfe_ref").ToString.Trim
                micfe.Referencia.ReferenciaA.Item(0).NroCFERef = CInt(cab.Rows(0).Item("nro_cfe_ref"))
                micfe.Referencia.ReferenciaA.Item(0).RazonRef = "Referencia a eRemito de exportación"
                fechaAux = cab.Rows(0).Item("fec_fiscal_ref")
                micfe.Referencia.ReferenciaA.Item(0).FechaCFEref = New LiquidTechnologies.Runtime.Net40.XmlDateTime(fechaAux)
            Else
                micfe.Referencia.ReferenciaA.Item(0).IndGlobal = Enumerations.ReferenciaA_IndGlobal.n1 'referencia global
                If Trim(cab.Rows(0).Item("nro_cfe_ref").ToString) = "0" Then
                    micfe.Referencia.ReferenciaA.Item(0).RazonRef = "Referencia a documento no determinado, " & cab.Rows(0).Item("cod_doc_ref").ToString.Trim
                Else
                    micfe.Referencia.ReferenciaA.Item(0).RazonRef = "Referencia a documento no electrónico: código: " & cab.Rows(0).Item("cod_doc_ref").ToString & " serie: " & cab.Rows(0).Item("serie_cfe_ref").ToString & " número: " & cab.Rows(0).Item("nro_cfe_ref").ToString
                End If
            End If
        End If
        '----------------------------------------------------------------------------------


        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        '''''''''''''''''''''''''''''''''''''''''''''''''''' ADENDA '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        Me.adenda = Trim(cab.Rows(0).Item("adenda_export").ToString)

        Return 0
    End Function
    Private Function msgbox(ByVal msg As String) As Boolean
        Dim oex As New Exception(msg)
        Throw oex
    End Function

End Class


