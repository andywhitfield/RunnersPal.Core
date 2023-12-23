alter table UserAccountAuthentication add column CredentialId blob;
alter table UserAccountAuthentication add column PublicKey blob;
alter table UserAccountAuthentication add column UserHandle blob;
alter table UserAccountAuthentication add column SignatureCount integer;
alter table UserAccountAuthentication drop column Identifier;
