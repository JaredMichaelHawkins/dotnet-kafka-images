#!/bin/bash
# topic.sh

KAFKA_BROKER=broker:29092

echo "Creating Kafka topic..."


kafka-topics \
  --create \
  --bootstrap-server $KAFKA_BROKER \
  --replication-factor 1 \
  --partitions 1 \
  --topic poc_topic


echo "Kafka topic is created."
sleep infinity
