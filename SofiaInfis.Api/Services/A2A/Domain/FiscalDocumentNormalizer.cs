using System.Globalization;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;
using SofiaInfis.Api.DTOs.A2A;

namespace SofiaInfis.Api.Services.A2A.Domain;

internal static class FiscalDocumentNormalizer
{
    private static string Text(JsonNode? node, string key) => RobertoLoteAdapter.Text(node, key);
    public static JsonObject? Normalize(DocumentoEntrada doc, JsonObject report, List<string> issues)
    {
        try
        {
            if (doc.texto.TrimStart().StartsWith('<')) return Nfe(doc, report);
            if (doc.texto.TrimStart().StartsWith("|0000|")) return Efd(doc, report);
            throw new FormatException("tipo de documento sem normalização automática; forneça os registros já normalizados no contrato lote.v1.");
        }
        catch (Exception ex) when (ex is FormatException or XmlException or InvalidOperationException or ArgumentException)
        {
            // Parser exception text may contain document values. Only controlled format messages are exposed.
            issues.Add($"{doc.nome}: " + (ex is FormatException ? ex.Message : "estrutura fiscal inválida ou ambígua para normalização."));
            return null;
        }
    }
    private static JsonObject Nfe(DocumentoEntrada doc, JsonObject report)
    {
        using var reader = XmlReader.Create(new StringReader(doc.texto), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 20000 });
        var xml = XDocument.Load(reader);
        var invoices = xml.Descendants().Where(x => x.Name.LocalName == "infNFe").ToArray();
        if (invoices.Length != 1) throw new FormatException("é necessária uma NF-e por arquivo; NFS-e e outros XML devem vir normalizados no lote.v1.");
        var info = invoices[0];
        string Field(string group, string field) => info.Elements().SingleOrDefault(x => x.Name.LocalName == group)?.Descendants().SingleOrDefault(x => x.Name.LocalName == field)?.Value ?? "";
        var version = info.Attribute("versao")?.Value;
        if (version != "4.00" || Field("ide", "mod") != "55") throw new FormatException("somente NF-e modelo 55, layout 4.00, possui normalização XML automática.");
        var cnpj = Text(report["contribuinte"], "cnpj");
        if (!RobertoLoteAdapter.ValidCnpj(cnpj) || cnpj != Field("emit", "CNPJ") && cnpj != Field("dest", "CNPJ"))
            throw new FormatException("relatório precisa identificar o CNPJ analisado no papel de emitente ou destinatário.");
        var emission = Field("ide", "dhEmi");
        if (emission.Length < 10 || !DateOnly.TryParseExact(emission[..10], "yyyy-MM-dd", out _)) throw new FormatException("data de emissão ausente ou inválida.");
        var identity = info.Attribute("Id")?.Value ?? "";
        if (!identity.StartsWith("NFe", StringComparison.Ordinal)) throw new FormatException("chave de acesso declarada no XML ausente.");
        var records = new JsonObject();
        foreach (var group in info.Descendants().Where(x => x.HasElements && x.Elements().Any(e => !e.HasElements)))
        {
            // Preserve actual XML group/field names, repeated occurrences and textual values.
            if (group.Name.NamespaceName == "http://www.w3.org/2000/09/xmldsig#") continue;
            var fields = new JsonObject();
            foreach (var element in group.Elements().Where(x => !x.HasElements))
            {
                if (fields.ContainsKey(element.Name.LocalName)) throw new FormatException("campos repetidos no mesmo grupo XML impedem normalização inequívoca.");
                fields[element.Name.LocalName] = element.Value;
            }
            Add(records, group.Name.LocalName, fields, null);
        }
        return Obligation(doc, "NFE", version, report["contribuinte"]!, emission[..10], emission[..10], "ORIGINAL",
            new JsonObject { ["chaveAcesso"] = identity[3..] }, report["validacaoOrigem"], records);
    }

    // Reviewed positional maps for EFD ICMS/IPI 020. Unknown data records require normalized input.
    private static readonly Dictionary<string, string[]> EfdFields = new()
    {
        ["0000"] = "COD_VER COD_FIN DT_INI DT_FIN NOME CNPJ CPF UF IE COD_MUN IM SUFRAMA IND_PERFIL IND_ATIV".Split(' '),
        ["0150"] = "COD_PART NOME COD_PAIS CNPJ CPF IE COD_MUN SUFRAMA END NUM COMPL BAIRRO".Split(' '),
        ["C100"] = "IND_OPER IND_EMIT COD_PART COD_MOD COD_SIT SER NUM_DOC CHV_NFE DT_DOC DT_E_S VL_DOC IND_PGTO VL_DESC VL_ABAT_NT VL_MERC IND_FRT VL_FRT VL_SEG VL_OUT_DA VL_BC_ICMS VL_ICMS VL_BC_ICMS_ST VL_ICMS_ST VL_IPI VL_PIS VL_COFINS VL_PIS_ST VL_COFINS_ST".Split(' '),
        ["C190"] = "CST_ICMS CFOP ALIQ_ICMS VL_OPR VL_BC_ICMS VL_ICMS VL_BC_ICMS_ST VL_ICMS_ST VL_RED_BC VL_IPI COD_OBS".Split(' '),
        ["1601"] = "COD_PART_IP COD_PART_IT TOT_VS TOT_ISS TOT_OUTROS".Split(' ')
    };
    private static JsonObject Efd(DocumentoEntrada doc, JsonObject report)
    {
        var lines = doc.texto.Split('\n').Select(x => x.TrimEnd('\r')).Where(x => x.Length > 0).ToArray();
        var records = new JsonObject();
        for (var i = 0; i < lines.Length; i++)
        {
            var cells = lines[i].Split('|');
            if (cells.Length < 3 || cells[0] != "" || cells[^1] != "") throw new FormatException("estrutura delimitada SPED inválida.");
            var code = cells[1];
            if (!EfdFields.TryGetValue(code, out var names))
            {
                // Only empty/opening/closing/count records can be omitted from a fiscal cross-check.
                if (code.Length == 4 && (code.EndsWith("001") || code.EndsWith("990") || code is "9001" or "9900" or "9990" or "9999")) continue;
                throw new FormatException("há registros SPED sem mapeamento automático revisado; forneça o arquivo já normalizado para preservar sua semântica.");
            }
            if (cells.Length - 3 != names.Length) throw new FormatException("quantidade de campos incompatível com o layout EFD ICMS/IPI 020.");
            var fields = new JsonObject();
            for (var j = 0; j < names.Length; j++)
            {
                var value = cells[j + 2];
                if (names[j].StartsWith("DT_") && value.Length > 0) value = Date(value);
                else if (names[j].StartsWith("VL_") || names[j].StartsWith("TOT_") || names[j] == "ALIQ_ICMS")
                {
                    if (value.Length > 0)
                    {
                        if (!decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.GetCultureInfo("pt-BR"), out var number)) throw new FormatException("valor decimal inválido no SPED.");
                        value = number.ToString(CultureInfo.InvariantCulture);
                    }
                }
                fields[names[j]] = value.Length == 0 ? null : JsonValue.Create(value);
            }
            Add(records, code, fields, i + 1);
        }
        if (records["0000"] is not JsonArray { Count: 1 } header) throw new FormatException("registro 0000 ausente ou duplicado.");
        var first = header[0]!["campos"]!;
        if (Text(first, "COD_VER") != "020") throw new FormatException("extração SPED automática disponível somente para EFD ICMS/IPI 020; demais tipos precisam vir normalizados.");
        var purpose = Text(first, "COD_FIN") switch { "0" => "ORIGINAL", "1" => "SUBSTITUTA", _ => throw new FormatException("finalidade da EFD não declarada.") };
        return Obligation(doc, "EFD_ICMS_IPI", "020", new JsonObject { ["cnpj"] = Text(first, "CNPJ"), ["uf"] = Text(first, "UF"), ["inscricaoEstadual"] = Text(first, "IE") },
            Text(first, "DT_INI"), Text(first, "DT_FIN"), purpose, report["identificacao"], report["validacaoOrigem"], records);
    }
    private static string Date(string value)
    {
        if (!DateOnly.TryParseExact(value, "ddMMyyyy", out var date)) throw new FormatException("data SPED ausente ou inválida.");
        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
    private static void Add(JsonObject records, string code, JsonObject fields, int? line)
    {
        if (records[code] is not JsonArray) records[code] = new JsonArray();
        records[code]!.AsArray().Add(new JsonObject { ["linha"] = line, ["campos"] = fields });
    }
    private static JsonObject Obligation(DocumentoEntrada doc, string type, string version, JsonNode taxpayer,
        string start, string end, string purpose, JsonNode? identity, JsonNode? validation, JsonObject records) => new()
    {
        ["id"] = doc.idDocumento, ["tipo"] = type, ["versaoLayout"] = version,
        ["contribuinte"] = taxpayer.DeepClone(), ["periodo"] = new JsonObject { ["inicio"] = start, ["fim"] = end },
        ["finalidade"] = purpose, ["identificacao"] = identity?.DeepClone(), ["validacaoOrigem"] = validation?.DeepClone(), ["registros"] = records
    };
}
