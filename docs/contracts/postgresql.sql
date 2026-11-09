-- Проект схемы. В демонстрации PostgreSQL не развёрнут.
CREATE TABLE shipment (
 tracking_number varchar(14) PRIMARY KEY,
 CHECK (tracking_number ~ '^LH-[0-9]{4}-[0-9]{6}$')
);
CREATE TABLE tracking_event (
 source varchar(32) NOT NULL,
 event_id varchar(32) NOT NULL,
 tracking_number varchar(14) NOT NULL REFERENCES shipment(tracking_number),
 status varchar(16) NOT NULL CHECK(status IN ('received','inTransit','delivered')),
 warehouse_id varchar(3) NOT NULL,
 occurred_at timestamptz NOT NULL,
 weight_kg numeric(8,3) NOT NULL CHECK(weight_kg > 0 AND weight_kg <= 10000),
 PRIMARY KEY(source,event_id)
);
CREATE INDEX ix_tracking_time ON tracking_event(tracking_number,occurred_at DESC);
