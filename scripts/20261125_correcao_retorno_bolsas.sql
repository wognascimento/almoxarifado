-- Corrige o bloqueio indevido dos campos preenchidos pelo gatilho de auditoria.
-- Aplicar no banco do exercicio apos os scripts de baixa e entrada de bolsas.
-- Nao altera quantidades nem movimentacoes existentes.
BEGIN;
CREATE OR REPLACE FUNCTION almoxarifado_apoio.proteger_bolsa_baixada()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF TG_OP <> 'INSERT' AND OLD.baixa_movimentacao IS NOT NULL THEN
        IF TG_OP = 'DELETE' THEN RAISE EXCEPTION 'Item de bolsa ja baixado. Exclusao bloqueada.'; END IF;
        IF (to_jsonb(NEW) - ARRAY['retorno','data_retorno','quantidade_retorno','retorno_por','retorno_em','entrada_estoque_qtde','alterado_por','alterado_data'])
            IS DISTINCT FROM
           (to_jsonb(OLD) - ARRAY['retorno','data_retorno','quantidade_retorno','retorno_por','retorno_em','entrada_estoque_qtde','alterado_por','alterado_data']) THEN
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
COMMIT;

