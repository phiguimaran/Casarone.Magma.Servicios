Imports Magma.Tools.Extensiones.Common.Servicios.Interfaces
Imports Magma.Tools.Extensiones.Common.Servicios.Entities
Imports Magma.Tools.Extensiones.Common.Servicios.ResolverEntrada
Imports Magma.Tools.Core.Business.DataServices
Imports Magma.Tools.Core.Common.Security

Public Class EnviarCFE
    Implements IServiciosServidor
    Private _entradaresuelta As String
    Private _transaccion As System.Data.IDbTransaction
    Private trans As String
    Private tipoCFE As String
    Private cfeXML As CFEXML = New CFEXML()
    Private respBody As serviciocfe.RespBody
    Private tipoMensaje As Integer = 310
    Private esBatch As Boolean = False

    Public Overloads Function Ejecutar(ByVal Entrada As Object, ByVal Transaccion As System.Data.IDbTransaction) As Object Implements IServiciosServidor.Ejecutar
        Dim result As Boolean
        Dim _cnxString As String
        Dim _cnxSql As System.Data.IDbConnection

        ' Si se llama desde un formulario, ya viene con la transacción del mismo
        ' Si la transacción está vacia es porque se llama de algun proceso no transaccional y hay que crear una y comitearla al final (por ejemplo la generación de intereses)
        If Transaccion Is Nothing Then
            _cnxString = CType(System.Threading.Thread.CurrentPrincipal, MagmaPrincipal).StringConnection.Trim()
            _cnxSql = New System.Data.OleDb.OleDbConnection(_cnxString)

            If _cnxString.ToUpper.Contains("DNS=") Then
                _cnxSql = New System.Data.Odbc.OdbcConnection(_cnxString)
            ElseIf MagmaPrincipal.CurrentBackend() = BackendType.Informix Then
                _cnxSql = New System.Data.OleDb.OleDbConnection(_cnxString)
            Else
                _cnxSql = New System.Data.SqlClient.SqlConnection(_cnxString)
            End If
            _cnxSql.Open()
            _transaccion = _cnxSql.BeginTransaction()
            result = Ejecutar(Entrada)
            If result Then
                _transaccion.Commit()
                _cnxSql.Close()
                _cnxSql = Nothing
            Else
                _transaccion.Rollback()
                _cnxSql.Close()
                _cnxSql = Nothing
            End If
        Else
            _transaccion = Transaccion
            result = Ejecutar(Entrada)
        End If
        Return result
    End Function


    Public Function EntradaResuelta() As String Implements IServiciosServidor.EntradaResuelta
        Return _entradaresuelta
    End Function

    Private Function msgbox(ByVal msg As String) As Boolean
        Dim oex As New Exception(msg)
        Throw oex
    End Function

    Public Overloads Function Ejecutar(ByVal Entrada As Object) As Object Implements IServiciosServidor.Ejecutar

        Dim entradas As String()
        Dim tiempo As Double

        tiempo = Timer

        'cambia los @ de los parametros, por sus valores y devuelve todo el string separado por pipes
        _entradaresuelta = ResolverEntrada(Entrada, TipoFormato.gkintFormatoNO)
        entradas = _entradaresuelta.Split("|")
        tipoCFE = entradas(0)
        trans = entradas(1)
        If trans < 3766000 Then
            msgbox("Comprobante anterior al cambio de sistema. No es posible editar de esta forma.")
        End If
        esBatch = IIf(entradas(2) = "S", True, False)
        tipoMensaje = IIf(esBatch, 340, 310)
        'Return True
        Select Case cfeXML.ObtenerDatos(_transaccion, tipoCFE, trans)
            Case 0
                ' todo ok
            Case 1
                msgbox("Falta definir parrámetro 'rut' en '/Contabilidad/Tablas/Parametros por empresas'")
                Return False
            Case 2
                msgbox("Flta definir parrámetro 'emisor' en '/Contabilidad/Tablas/Parametros por empresas'")
                Return False
            Case 3
                msgbox("Flta definir parrámetro 'codcomercio' en '/Contabilidad/Tablas/Parametros por empresas'")
                Return False
            Case 4
                msgbox("Flta definir parrámetro 'codterminal' en '/Contabilidad/Tablas/Parametros por empresas'")
                Return False
            Case 5
                msgbox("No se encontró el cabezal de la factura para la transacción " & trans)
                Return False
            Case 6
                msgbox("No se encontraron las lineas de la factura para la transacción " & trans)
                Return False
            Case 7
                msgbox("No se encontró la sucursal indicada en la factura para la transacción " & trans)
                Return False
            Case 8
                msgbox("No se encontró el país del cliente indicado en la factura para la transacción " & trans & ". Ver '/Contabilidad/Tablas/Paises'")
                Return False
            Case 9
                msgbox("No se encontró el codigo iso 3166 del país del cliente indicado en la factura para la transacción " & trans & ". Ver '/Contabilidad/Tablas/Paises'")
                Return False
            Case 10 ' hasta aca son comunes a plaza y export
                msgbox("Tipo de comprobante no contemplado")
                Return False
            Case 11 'este es solo para plaza
                msgbox("tasa de IVA desconocida")
                Return False
            Case 12 'este es solo para export
                ' Es una factura de exportaqción pero no es fiscal -> no hay que hacer nada
                Return True
            Case 13 'este es solo para export
                ' Es una NC de exportaqción asociada a una factura que NO es fiscal -> Error ya que no puedo generar la referencia
                msgbox("La factura referenciada NO es fiscal. No puede hacerle una nota de crédito a una factura no contabilizada.")
                Return False
            Case 14 'este es solo para export !!!! FALTA ESTE RETURN 
                ' Es un eRemito de exportacion de contingencia y no indicaron la serie o el numero de CFE
                msgbox("Este es un documento de contingencia, DEBE indicar la serie y el numero")
                Return False
            Case -8
                msgbox("hasta aca llega")   ' esto está para testing
                Return False
            Case Else
                msgbox("Error no determinado al acceder a los datos de la factura para la transacción " & trans)
                Return False
        End Select
        Select Case cfeXML.Invocar(tipoMensaje, tipoCFE, trans, respBody)
            Case 0
                ' todo ok msgbox("00-CFE enviado y aceptado por DGI. " & respBody.Resp.MensajeRta)
            Case 1
                msgbox("01-Denegado. " & respBody.Resp.MensajeRta)
                Return False
            Case 3
                msgbox("03-Comercio inválido. " & respBody.Resp.MensajeRta)
                Return False
            Case 5
                msgbox("05-CFE Rechazado por DGI. " & respBody.Resp.MensajeRta)
                Return False
            Case 11
                'todo ok msgbox("11-CFE aceptado por UCFE, pero aun no se ha completado el envío a DGI." & respBody.Resp.MensajeRta)
            Case 12
                msgbox("12-Requerimiento inválido. " & respBody.Resp.MensajeRta)
                Return False
            Case 30
                msgbox("30-Error en formato. " & respBody.Resp.MensajeRta)
                Return False
            Case 31
                msgbox("31-Error en formato de CFE. " & respBody.Resp.MensajeRta)
                Return False
            Case 89
                msgbox("89-Terminal inválida. " & respBody.Resp.MensajeRta)
                Return False
            Case 94
                msgbox("94-Identificado de requerimiento duplicado, repita incrementando en uno. " & respBody.Resp.MensajeRta)
                Return False
            Case 96
                msgbox("96-Error en sistema. " & respBody.Resp.MensajeRta)
                Return False
            Case 99
                msgbox("99-Sesión no iniciada. " & respBody.Resp.MensajeRta)
                Return False
            Case Is < 100
                msgbox(respBody.Resp.CodRta & "-Otro. " & respBody.Resp.MensajeRta)
                Return False
            Case 100
                msgbox("100-El código HMAC o concuerda. " & respBody.ErrorMessage)
                Return False
            Case 101
                msgbox("101-No se han podido interpretar los datos descifrados. " & respBody.ErrorMessage)
                Return False
            Case 102
                msgbox("102-No hay una llave de descifrado configurada para la empresa. " & respBody.ErrorMessage)
                Return False
            Case 200
                msgbox("200-Se ha detectado una repetición. Requerimiento procesado y se ha informado al administrador. " & respBody.ErrorMessage)
                Return False
            Case 201
                msgbox("201-Se ha detectado una repetición. Requerimiento rechazado y se ha informado al administrador. " & respBody.ErrorMessage)
                Return False
            Case 300
                msgbox("300-Debe enviar el codigo de comercio. " & respBody.ErrorMessage)
                Return False
            Case 301
                msgbox("301-Codigo de comercio no concuerda con el del requerimiento. " & respBody.ErrorMessage)
                Return False
            Case 302
                msgbox("302-Codigo de terminal no concuerda con el del requerimiento. " & respBody.ErrorMessage)
                Return False
            Case 400
                msgbox("400-Timeout sin que UCFE pudiera procesar el requerimiento. " & respBody.ErrorMessage)
                Return False
            Case 500
                msgbox("500-Error interno. " & respBody.ErrorMessage)
                Return False
            Case Else
                msgbox(Str(respBody.ErrorCode) & "-Otro. " & respBody.ErrorMessage)
                Return False
        End Select
        'msgbox("luego de invocar")
        Try
            tiempo = Timer - tiempo
            cfeXML.Almacenar(_transaccion, respBody, tiempo.ToString, tipoCFE)
            ' no puedo dar ningún error porque ya se mandó el comprobante a DGI, es preferible que no se almacene algún dato 
            ' de las tablas auxiliares edocumentos o edocumentos2 antes que el comprobante no quede registrado en Link y si enviado a DGI
        Catch
        End Try
        cfeXML = Nothing
        respBody = Nothing

        Return True

    End Function
End Class

