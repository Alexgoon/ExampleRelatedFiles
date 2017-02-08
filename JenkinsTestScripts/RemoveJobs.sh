COUNTER=6
while [  $COUNTER -lt 1000 ]; do
	curl -X POST http://localhost:8080/job/Item$COUNTER/doDelete
	let COUNTER=COUNTER+1 
done
echo "ready"
read