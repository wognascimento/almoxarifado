-- Executar no banco do exercicio antes de publicar esta versao. Nao gera baixas retroativas.
BEGIN;
LOCK TABLE almoxarifado_jac.t_saida_almox IN SHARE ROW EXCLUSIVE MODE;
CREATE SEQUENCE IF NOT EXISTS almoxarifado_jac.seq_movimentacao_saida AS integer;
SELECT setval('almoxarifado_jac.seq_movimentacao_saida',
    GREATEST(COALESCE((SELECT MAX(cod_movimentacao) FROM almoxarifado_jac.t_saida_almox),0),
        (SELECT last_value FROM almoxarifado_jac.seq_movimentacao_saida)),true);
-- O DBA deve conceder USAGE nesta sequencia aos mesmos perfis que ja registram saidas.
-- GRANT USAGE ON SEQUENCE almoxarifado_jac.seq_movimentacao_saida TO <perfil_do_almoxarifado>;
ALTER TABLE almoxarifado_apoio.tblsaidabolsa
    ADD COLUMN IF NOT EXISTS baixa_movimentacao integer,
    ADD COLUMN IF NOT EXISTS baixa_saida_id integer REFERENCES almoxarifado_jac.t_saida_almox(cod_saida_almox),
    ADD COLUMN IF NOT EXISTS baixa_em timestamp with time zone,
    ADD COLUMN IF NOT EXISTS baixa_por varchar(50);

CREATE OR REPLACE FUNCTION almoxarifado_apoio.proteger_bolsa_baixada()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF TG_OP <> 'INSERT' AND OLD.baixa_movimentacao IS NOT NULL THEN
        IF TG_OP = 'DELETE' THEN
            RAISE EXCEPTION 'Item de bolsa ja baixado no estoque. Exclusao bloqueada.';
        END IF;
        -- Devolucoes continuam no fluxo Bolsa Entrada; os dados de saida sao imutaveis.
        IF (to_jsonb(NEW) - ARRAY['retorno','data_retorno','quantidade_retorno','retorno_por','retorno_em','alterado_por','alterado_data'])
            IS DISTINCT FROM
           (to_jsonb(OLD) - ARRAY['retorno','data_retorno','quantidade_retorno','retorno_por','retorno_em','alterado_por','alterado_data']) THEN
            RAISE EXCEPTION 'Item de bolsa ja baixado no estoque. Alteracao bloqueada.';
        END IF;
    END IF;
    IF TG_OP = 'INSERT' THEN
        IF NEW.baixa_movimentacao IS NOT NULL OR NEW.baixa_saida_id IS NOT NULL THEN
            RAISE EXCEPTION 'Inclua o item antes de efetuar a baixa.';
        END IF;
        IF EXISTS (SELECT 1 FROM almoxarifado_apoio.tblsaidabolsa b
            WHERE b.codfun = NEW.codfun AND b.codigo_bolsa = NEW.codigo_bolsa
              AND b.destino_shop IS NOT DISTINCT FROM NEW.destino_shop
              AND b.baixa_movimentacao IS NOT NULL) THEN
            RAISE EXCEPTION 'Bolsa ja baixada no estoque. Inclusao bloqueada.';
        END IF;
    END IF;
    IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
    RETURN NEW;
END;
$$;
DROP TRIGGER IF EXISTS trg_proteger_bolsa_baixada ON almoxarifado_apoio.tblsaidabolsa;
CREATE TRIGGER trg_proteger_bolsa_baixada BEFORE INSERT OR UPDATE OR DELETE
ON almoxarifado_apoio.tblsaidabolsa FOR EACH ROW
EXECUTE FUNCTION almoxarifado_apoio.proteger_bolsa_baixada();
COMMIT;
