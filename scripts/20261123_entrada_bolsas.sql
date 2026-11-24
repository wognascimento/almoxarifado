-- Aplicar depois de 20261123_baixa_bolsas.sql, com as estacoes fechadas.
BEGIN;
LOCK TABLE almoxarifado_jac.t_entrada_almox IN SHARE ROW EXCLUSIVE MODE;
CREATE SEQUENCE IF NOT EXISTS almoxarifado_jac.seq_movimentacao_entrada AS integer;
SELECT setval('almoxarifado_jac.seq_movimentacao_entrada',GREATEST(
    COALESCE((SELECT MAX(cod_movimentacao) FROM almoxarifado_jac.t_entrada_almox),0),
    (SELECT last_value FROM almoxarifado_jac.seq_movimentacao_entrada)),true);
ALTER TABLE almoxarifado_apoio.tblsaidabolsa ADD COLUMN IF NOT EXISTS entrada_estoque_qtde numeric NOT NULL DEFAULT 0;
CREATE TABLE IF NOT EXISTS almoxarifado_apoio.tblbolsa_entradas_estoque (
    cod_controlelinha integer NOT NULL REFERENCES almoxarifado_apoio.tblsaidabolsa(cod_controlelinha),
    cod_entrada_almox integer NOT NULL REFERENCES almoxarifado_jac.t_entrada_almox(cod_entrada_almox),
    quantidade numeric NOT NULL CHECK (quantidade > 0 AND quantidade < 'Infinity'::numeric),
    registrado_em timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    registrado_por varchar(50) NOT NULL,
    PRIMARY KEY (cod_controlelinha,cod_entrada_almox)
);

CREATE OR REPLACE FUNCTION almoxarifado_apoio.proteger_bolsa_baixada()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF TG_OP <> 'INSERT' AND OLD.baixa_movimentacao IS NOT NULL THEN
        IF TG_OP = 'DELETE' THEN RAISE EXCEPTION 'Item de bolsa ja baixado. Exclusao bloqueada.'; END IF;
        IF (to_jsonb(NEW) - ARRAY['retorno','data_retorno','quantidade_retorno','retorno_por','retorno_em','entrada_estoque_qtde'])
            IS DISTINCT FROM
           (to_jsonb(OLD) - ARRAY['retorno','data_retorno','quantidade_retorno','retorno_por','retorno_em','entrada_estoque_qtde']) THEN
            RAISE EXCEPTION 'Item de bolsa ja baixado. Alteracao bloqueada.';
        END IF;
    END IF;
    IF TG_OP = 'INSERT' THEN
        IF NEW.baixa_movimentacao IS NOT NULL OR NEW.baixa_saida_id IS NOT NULL THEN
            RAISE EXCEPTION 'Inclua o item antes da baixa.';
        END IF;
        IF EXISTS (SELECT 1 FROM almoxarifado_apoio.tblsaidabolsa b WHERE b.codfun=NEW.codfun
            AND b.codigo_bolsa=NEW.codigo_bolsa AND b.destino_shop IS NOT DISTINCT FROM NEW.destino_shop
            AND b.baixa_movimentacao IS NOT NULL) THEN
            RAISE EXCEPTION 'Bolsa ja baixada. Inclusao bloqueada.';
        END IF;
    END IF;
    IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
    RETURN NEW;
END;
$$;

CREATE OR REPLACE FUNCTION almoxarifado_apoio.validar_retorno_estoque_bolsa()
RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE total numeric;
BEGIN
    SELECT COALESCE(SUM(quantidade),0) INTO total
    FROM almoxarifado_apoio.tblbolsa_entradas_estoque WHERE cod_controlelinha=NEW.cod_controlelinha;
    IF NEW.entrada_estoque_qtde IS DISTINCT FROM total THEN
        RAISE EXCEPTION 'Quantidade de entrada diverge do historico de estoque.';
    END IF;
    IF TG_OP='UPDATE' AND OLD.entrada_estoque_qtde>0 AND
        (NEW.codfun,NEW.codigo_bolsa,NEW.codcompladicional,NEW.destino_shop,NEW.quantidade)
        IS DISTINCT FROM (OLD.codfun,OLD.codigo_bolsa,OLD.codcompladicional,OLD.destino_shop,OLD.quantidade) THEN
        RAISE EXCEPTION 'Item com entrada registrada. Dados de origem bloqueados.';
    END IF;
    IF COALESCE(NEW.quantidade_retorno,0)::numeric < total
        OR COALESCE(NEW.quantidade_retorno,0)<0
        OR NEW.quantidade_retorno::text IN ('NaN','Infinity','-Infinity')
        OR (COALESCE(NEW.quantidade_retorno,0)>0 AND
            (NEW.quantidade IS NULL OR NEW.quantidade_retorno>NEW.quantidade)) THEN
        RAISE EXCEPTION 'Retorno deve estar entre a entrada ja registrada e a quantidade enviada.';
    END IF;
    RETURN NEW;
END;
$$;
DROP TRIGGER IF EXISTS trg_validar_retorno_estoque_bolsa ON almoxarifado_apoio.tblsaidabolsa;
CREATE TRIGGER trg_validar_retorno_estoque_bolsa BEFORE INSERT OR UPDATE
ON almoxarifado_apoio.tblsaidabolsa FOR EACH ROW
EXECUTE FUNCTION almoxarifado_apoio.validar_retorno_estoque_bolsa();
-- Conceder aos perfis de almoxarifado: USAGE na sequencia; SELECT, INSERT na tabela de historico.
COMMIT;
